#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal;

// Lightweight awaitable reset event
internal sealed class AsyncResetEvent
{
	private volatile TaskCompletionSource<bool> _tcs =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	public Task WaitAsync(CancellationToken ct = default)
	{
		var t = _tcs.Task;
		return ct.CanBeCanceled ? t.WaitAsync(ct) : t;
	}
	public void Set()
	{
		var t = _tcs;
		if (!t.Task.IsCompleted) t.TrySetResult(true);
	}
	public void Reset()
	{
		if (_tcs.Task.IsCompleted)
		{
			Interlocked.CompareExchange(ref _tcs,
				new(TaskCreationOptions.RunContinuationsAsynchronously), _tcs);
		}
	}
}

public sealed class ScreenChangedEventArgs : EventArgs
{
	public int Version { get; }
	public DateTime UtcCommittedAt { get; }
	public ScreenChangedEventArgs(int version, DateTime utcCommittedAt)
	{ Version = version; UtcCommittedAt = utcCommittedAt; }
}

/// <summary>
/// Double-buffered terminal screen with:
///  - O(1) atomic swap on commit (readers never block).
///  - Events on screen change.
///  - Blocking waits for N refreshes.
///  - "Stability" detection (debounced): IsStable/IsUnstable + WaitUntilStableAsync().
/// </summary>
public sealed class TerminalScreenDb 
{
	public int Rows { get; }
	public int Cols { get; }

	private volatile char[] _front;
	private volatile char[] _back;

	private readonly SemaphoreSlim _buildLock = new(1, 1);

	private readonly char _blank;
	private int _version;
	private readonly AsyncResetEvent _versionEvent = new();

	// ---- Stability (debounce) state ----
	/// <summary>
	/// Duration with no commits after which the screen is considered stable.
	/// Changeable at runtime; default 200 ms.
	/// </summary>
	public TimeSpan StableAfter
	{
		get => TimeSpan.FromTicks(Volatile.Read(ref _stableAfterTicks));
		set
		{
			if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(value));
			Interlocked.Exchange(ref _stableAfterTicks, value.Ticks);
			// Re-arm stability timer using new window if currently unstable:
			if (!IsStable) RestartStabilityCountdown();
		}
	}
	private long _stableAfterTicks = TimeSpan.FromMilliseconds(200).Ticks;

	// Last commit time (UTC ticks)
	private long _lastCommitTicks;

	// Exposed stability flags
	public bool IsStable
	{
		get
		{
			var since = DateTime.UtcNow.Ticks - Volatile.Read(ref _lastCommitTicks);
			return since >= Volatile.Read(ref _stableAfterTicks);
		}
	}
	public bool IsUnstable => !IsStable;

	private readonly AsyncResetEvent _stableEvent = new(); // set when stable, reset on commit
	private readonly object _stableLock = new();
	private CancellationTokenSource? _stableCts; // canceled on each commit to debounce the timer

	// ----------- Change notifications -----------
	public event Action<int>? ScreenChangedSimple;
	public event EventHandler<ScreenChangedEventArgs>? ScreenChanged;

	public TerminalScreenDb(int rows = 25, int cols = 80, char blank = ' ')
	{
		if (rows <= 0 || cols <= 0) throw new ArgumentOutOfRangeException();
		Rows = rows; Cols = cols; _blank = blank;
		_front = new char[rows * cols];
		_back  = new char[rows * cols];
		Array.Fill(_front, _blank);
		Array.Fill(_back,  _blank);

		var now = DateTime.UtcNow.Ticks;
		_lastCommitTicks = now;
		_version = 1;

		_versionEvent.Set();
		_stableEvent.Set(); // startup considered stable
	}

	public int Version => Volatile.Read(ref _version);

	// ---------------------- READERS ----------------------
	public void ReadRun0(int row0, int col0, Span<char> dest)
	{
		ValidateRowCol0(row0, col0);
		var src = _front;
		var len = Math.Min(dest.Length, Cols - col0);
		if (len > 0) src.AsSpan(OffsetOf(row0, col0), len).CopyTo(dest);
	}

	public string ReadRun0String(int row0, int col0, int length, bool rtrim = true)
	{
		var tmp = length <= 256 ? stackalloc char[length] : new char[length];
		ReadRun0(row0, col0, tmp);
		var slice = tmp[..Math.Min(length, Cols - col0)];
		return rtrim ? slice.TrimEndSpaces().ToString() : new string(slice);
	}

	public int ReadRect0(int by0, int bx0, int ey0, int ex0, Span<char> dest, bool withNewLines = false)
	{
		NormalizeRect(ref bx0, ref by0, ref ex0, ref ey0);
		ValidateRect0(bx0, by0, ex0, ey0);
		var src = _front;
		int width = ex0 - bx0 + 1, lines = ey0 - by0 + 1;
		var perLine = width + (withNewLines ? 1 : 0);
		var required = perLine * lines;
		if (dest.Length < required) throw new ArgumentException("Destination too small.");
		var written = 0;
		for (var r = by0; r <= ey0; r++)
		{
			src.AsSpan(OffsetOf(r, bx0), width).CopyTo(dest.Slice(written, width));
			written += width;
			if (withNewLines) dest[written++] = '\n';
		}
		return written;
	}

	public string[] ReadRectLines0(int by0, int bx0, int ey0, int ex0, bool rtrim = true)
	{
		NormalizeRect(ref bx0, ref by0, ref ex0, ref ey0);
		ValidateRect0(bx0, by0, ex0, ey0);
		var src = _front;
		int width = ex0 - bx0 + 1, lines = ey0 - by0 + 1;
		var result = new string[lines];
		for (var i = 0; i < lines; i++)
		{
			var span = src.AsSpan(OffsetOf(by0 + i, bx0), width);
			result[i] = rtrim ? span.TrimEndSpaces().ToString() : new string(span);
		}
		return result;
	}

	// 1-based convenience
	public string ReadRun1String(int row1, int col1, int len, bool rtrim = true)
		=> ReadRun0String(row1 - 1, col1 - 1, len, rtrim);
	public string[] ReadRectLines1(int by1, int bx1, int ey1, int ex1, bool rtrim = true)
		=> ReadRectLines0(by1 - 1, bx1 - 1, ey1 - 1, ex1 - 1, rtrim);

	// ---------------------- WRITERS (DOUBLE-BUFFER BUILD) ----------------------
	public async Task<BuildSession> BeginBuildAsync(bool clearBackBuffer = true, CancellationToken ct = default)
	{
		await _buildLock.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (clearBackBuffer) Array.Fill(_back, _blank);
			else Array.Copy(_front, _back, _back.Length);
			return new BuildSession(this);
		}
		catch
		{
			_buildLock.Release();
			throw;
		}
	}

	internal void CommitSwap()
	{
		// Swap buffers atomically and bump version
		var oldFront = Interlocked.Exchange(ref _front, _back);
		_back = oldFront;
		var newVersion = Interlocked.Increment(ref _version);

		// Update commit time and mark UNSTABLE
		Interlocked.Exchange(ref _lastCommitTicks, DateTime.UtcNow.Ticks);
		_stableEvent.Reset();           // not stable immediately after a commit
		RestartStabilityCountdown();    // re-arm debounce timer

		// Wake version waiters
		_versionEvent.Set();
		_versionEvent.Reset();

		// Release write lock
		_buildLock.Release();

		// Fire notifications off the hot path
		var simple = ScreenChangedSimple;
		var ev     = ScreenChanged;
		if (simple is not null || ev is not null)
		{
			ThreadPool.UnsafeQueueUserWorkItem(_ =>
			{
				try { simple?.Invoke(newVersion); } catch { }
				try { ev?.Invoke(this, new ScreenChangedEventArgs(newVersion, DateTime.UtcNow)); } catch { }
			}, null);
		}
	}

	private void RestartStabilityCountdown()
	{
		CancellationTokenSource? prev;
		lock (_stableLock)
		{
			prev = _stableCts;
			_stableCts = new CancellationTokenSource();
		}
		prev?.Cancel();
		prev?.Dispose();

		var cts = _stableCts!;
		var delay = TimeSpan.FromTicks(Volatile.Read(ref _stableAfterTicks));

		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(delay, cts.Token).ConfigureAwait(false);
				// If not canceled by a subsequent commit, we are now stable.
				_stableEvent.Set();
			}
			catch (OperationCanceledException) { /* expected on re-commit */ }
		});
	}

	// ---------------------- Blocking waits ----------------------
	/// <summary>Block until <paramref name="refreshCount"/> future commits have occurred.</summary>
	public void WaitForRefreshes(int refreshCount, CancellationToken ct = default)
	{
		if (refreshCount <= 0) return;
		var target = Version + refreshCount;
		WaitForVersionSync(target, ct);
	}

	/// <summary>Await until <paramref name="refreshCount"/> future commits have occurred.</summary>
	public Task WaitForRefreshesAsync(int refreshCount, CancellationToken ct = default)
	{
		if (refreshCount <= 0) return Task.CompletedTask;
		var target = Version + refreshCount;
		return WaitForVersionAsync(target, ct);
	}

	/// <summary>Await until at least <paramref name="minVersion"/> is committed.</summary>
	public async Task WaitForVersionAsync(int minVersion, CancellationToken ct = default)
	{
		while (Version < minVersion)
		{
			await _versionEvent.WaitAsync(ct).ConfigureAwait(false);
			if (Version < minVersion) _versionEvent.Reset();
		}
	}

	private void WaitForVersionSync(int minVersion, CancellationToken ct)
	{
		while (Version < minVersion)
		{
			_versionEvent.WaitAsync(ct).GetAwaiter().GetResult();
			if (Version < minVersion) _versionEvent.Reset();
		}
	}

	/// <summary>
	/// Await until the screen becomes stable (no commits for at least <see cref="StableAfter"/>).
	/// If already stable, returns immediately.
	/// </summary>
	public async Task WaitUntilStableAsync(CancellationToken ct = default)
	{
		if (IsStable) return;
		await _stableEvent.WaitAsync(ct).ConfigureAwait(false);
	}

	/// <summary>
	/// Await until the screen is stable for a custom period (overriding <see cref="StableAfter"/> just for this call).
	/// </summary>
	public async Task WaitUntilStableAsync(TimeSpan customWindow, CancellationToken ct)
	{
		if (customWindow <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(customWindow));
		// Quick path: if already stable for at least customWindow, done.
		var sinceTicks = DateTime.UtcNow.Ticks - Volatile.Read(ref _lastCommitTicks);
		if (sinceTicks >= customWindow.Ticks) return;

		using var localCts = new CancellationTokenSource();
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, localCts.Token);

		// Mirror the debounce logic locally: wait until no commits occur for customWindow.
		// We subscribe by racing a delay against future commits via the global debounce restart.
		var observedVersion = Version;
		while (!linked.IsCancellationRequested)
		{
			// Wait either for a delay or for a new version, whichever comes first.
			var delayTask = Task.Delay(customWindow, linked.Token);
			var versionTask = WaitForVersionAsync(observedVersion + 1, linked.Token);
			var completed = await Task.WhenAny(delayTask, versionTask).ConfigureAwait(false);
			if (completed == delayTask) return; // no new commits during customWindow
			// New commit happened; update observedVersion and loop again.
			observedVersion = Version;
		}
	}

	// ---------------------- Internals ----------------------
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int OffsetOf(int row0, int col0) => (row0 * Cols) + col0;

	private void ValidateRowCol0(int row0, int col0)
	{
		if ((uint)row0 >= (uint)Rows) throw new ArgumentOutOfRangeException(nameof(row0));
		if ((uint)col0 >= (uint)Cols) throw new ArgumentOutOfRangeException(nameof(col0));
	}

	private void ValidateRect0(int bx0, int by0, int ex0, int ey0)
	{
		if (bx0 < 0 || by0 < 0 || ex0 >= Cols || ey0 >= Rows) throw new ArgumentOutOfRangeException("Rectangle out of bounds.");
		if (bx0 > ex0 || by0 > ey0) throw new ArgumentException("Invalid rectangle.");
	}

	private static void NormalizeRect(ref int bx0, ref int by0, ref int ex0, ref int ey0)
	{
		if (bx0 > ex0) (bx0, ex0) = (ex0, bx0);
		if (by0 > ey0) (by0, ey0) = (ey0, by0);
	}

	public readonly struct BuildSession : IAsyncDisposable, IDisposable
	{
		private readonly TerminalScreenDb _owner;
		internal BuildSession(TerminalScreenDb owner) { _owner = owner; }

		// Back-buffer write helpers
		public void Clear() => _owner.ClearBack();
		public void WriteSpan1(int row1, int col1, ReadOnlySpan<char> text) => _owner.WriteSpan0_Back(row1 - 1, col1 - 1, text);
		public void WriteSpan0(int row0, int col0, ReadOnlySpan<char> text) => _owner.WriteSpan0_Back(row0, col0, text);
		public void WriteField1(int row1, int col1, int length, ReadOnlySpan<char> text) => _owner.WriteField1_Back(row1, col1, length, text);
		public void FillRect0(int by0, int bx0, int ey0, int ex0, char ch) => _owner.FillRect0_Back(by0, bx0, ey0, ex0, ch);

		/// <summary>Swap buffers atomically (O(1)).</summary>
		public void Commit() => _owner.CommitSwap();

		public ValueTask DisposeAsync() { Commit(); return ValueTask.CompletedTask; }
		public void Dispose() => Commit();
	}

	// Back-buffer painters (internal)
	internal void ClearBack() => Array.Fill(_back, _blank);
	internal void WriteSpan0_Back(int row0, int col0, ReadOnlySpan<char> text)
	{
		ValidateRowCol0(row0, col0);
		var max = Math.Min(text.Length, Cols - col0);
		if (max <= 0) return;
		text[..max].CopyTo(_back.AsSpan(OffsetOf(row0, col0), max));
	}
	internal void WriteField1_Back(int row1, int col1, int length, ReadOnlySpan<char> text)
	{
		int row0 = row1 - 1, col0 = col1 - 1;
		ValidateRowCol0(row0, col0);
		if (length < 0 || col0 + length > Cols) throw new ArgumentOutOfRangeException(nameof(length));
		var start = OffsetOf(row0, col0);
		var toCopy = Math.Min(text.Length, length);
		var target = _back.AsSpan(start, length);
		if (toCopy > 0) text[..toCopy].CopyTo(target);
		if (toCopy < length) target.Slice(toCopy).Fill(_blank);
	}
	internal void FillRect0_Back(int by0, int bx0, int ey0, int ex0, char ch)
	{
		NormalizeRect(ref bx0, ref by0, ref ex0, ref ey0);
		ValidateRect0(bx0, by0, ex0, ey0);
		var width = ex0 - bx0 + 1;
		for (var r = by0; r <= ey0; r++)
		{
			_back.AsSpan(OffsetOf(r, bx0), width).Fill(ch);
		}
	}
}

internal static class SpanTrimExt
{
	[MethodImpl(
		MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<char> TrimEndSpaces(this ReadOnlySpan<char> span)
	{
		var end = span.Length - 1;
		while (end >= 0 && span[end] == ' ') end--;
		return span[..(end + 1)];
	}

	[MethodImpl(
		MethodImplOptions.AggressiveInlining)]
	public static Span<char> TrimEndSpaces(this Span<char> span)
	{
		var end = span.Length - 1;
		while (end >= 0 && span[end] == ' ') end--;
		return span[..(end + 1)];
	}
}
    
public interface ICharTranslator
{
	// Decode as many bytes as possible; write chars; return bytes consumed.
	int Decode(ReadOnlySpan<byte> input, Span<char> output, out int charsWritten);
}
/// <summary>
/// Minimal TN3270E parser (SBA + TEXT + IC + EUA + SF-ignore). Extend as needed.
/// Paints into the back buffer via a BuildSession, then swaps atomically.
/// </summary>
public sealed class Tn3270eAdapter
{
	private const byte SBA = 0x11; // Set Buffer Address (2 bytes)
	private const byte IC  = 0x13; // Insert Cursor (ignored)
	private const byte EUA = 0x12; // Erase Unprotected to Address (treated as space fill)
	private const byte SF  = 0x1D; // Start Field (ignored here)
	private const byte PT  = 0x05; // Program Tab (ignored)

	private static readonly bool[] _isOrder = BuildOrderTable();
	private static bool[] BuildOrderTable()
	{
		var t = new bool[256];
		t[SBA] = t[IC] = t[EUA] = t[SF] = t[PT] = true;
		return t;
	}

	private readonly TerminalScreenDb _screen;

	public Tn3270eAdapter(TerminalScreenDb screen) => _screen = screen;

	public async Task<int> ApplyDatastreamAsync(ReadOnlyMemory<byte> data, ICharTranslator translator,
		bool clearFirst = false, CancellationToken ct = default)
	{
		int bytesConsumedTotal = 0;
		await using var build = await _screen.BeginBuildAsync(clearFirst, ct).ConfigureAwait(false);

		int cols = _screen.Cols, rows = _screen.Rows;
		int cursor = 0; // linear index

		var span = data.Span;
		int i = 0;
		while (i < span.Length)
		{
			ct.ThrowIfCancellationRequested();
			byte b = span[i];

			if (b == SBA)
			{
				if (i + 2 >= span.Length) break;
				cursor = DecodeBA(span[i + 1], span[i + 2], rows, cols);
				i += 3; bytesConsumedTotal += 3;
				continue;
			}
			if (b == IC)
			{
				i += 1; bytesConsumedTotal += 1; continue;
			}
			if (b == EUA)
			{
				if (i + 2 >= span.Length) break;
				int to = DecodeBA(span[i + 1], span[i + 2], rows, cols);
				if (to >= cursor) FillLinear(ref cursor, to - cursor + 1, ' ');
				i += 3; bytesConsumedTotal += 3;
				continue;
			}
			if (b == SF)
			{
				// Skip attribute byte if present.
				if (i + 1 < span.Length) { i += 2; bytesConsumedTotal += 2; } else { i++; bytesConsumedTotal++; }
				continue;
			}

			// TEXT RUN
			int start = i;
			while (i < span.Length && !_isOrder[span[i]]) i++;
			if (i > start)
			{
				var chunk = span.Slice(start, i - start);
				int estimated = chunk.Length;
				Span<char> tmp = estimated <= 1024 ? stackalloc char[estimated] : new char[estimated];
				int charsWritten;
				int consumed = translator.Decode(chunk, tmp, out charsWritten);
				if (consumed > 0 && charsWritten > 0)
				{
					WriteLinear(tmp[..charsWritten], ref cursor);
					bytesConsumedTotal += consumed;
				}
				else
				{
					// Avoid infinite loop if translator returns 0 consumed
					bytesConsumedTotal += (i - start);
				}
			}
		}

		// Commit swap when leaving using / await using (O(1))
		return bytesConsumedTotal;

		// ----- Local helpers -----
		static int DecodeBA(byte b1, byte b2, int rows, int cols)
		{
			int a = ((b1 & 0x3F) << 6) | (b2 & 0x3F);
			int max = rows * cols;
			if ((uint)a >= (uint)max) a %= max; // wrap if host addresses beyond buffer
			return a;
		}

		void WriteLinear(ReadOnlySpan<char> text, ref int pos)
		{
			int max = rows * cols;
			while (!text.IsEmpty && pos < max)
			{
				int r = pos / cols, c = pos % cols;
				int space = cols - c;
				int toCopy = Math.Min(space, text.Length);
				build.WriteSpan0(r, c, text[..toCopy]);
				pos += toCopy;
				text = text[toCopy..];
			}
		}

		void FillLinear(ref int pos, int count, char ch)
		{
			int max = rows * cols;
			int end = Math.Min(max, pos + count);
			while (pos < end)
			{
				int r = pos / cols, c = pos % cols;
				int toLineEnd = Math.Min(cols - c, end - pos);
				build.FillRect0(r, c, r, c + toLineEnd - 1, ch);
				pos += toLineEnd;
			}
		}
	}
}
