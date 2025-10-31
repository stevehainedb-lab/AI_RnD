using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Open3270.TN3270;

/// <summary>
/// Async extensions for XmlScreen. These mirror existing synchronous APIs while leveraging
/// async I/O where possible. The original synchronous APIs remain for backward compatibility.
/// </summary>
public partial class Screen
{
	/// <summary>
	/// Returns this screen as an XML text string asynchronously.
	/// </summary>
	public Task<string> GetXmlTextAsync(bool useCache = true, CancellationToken cancellationToken = default)
	{
		// XmlSerializer is synchronous; offload serialization to a worker thread to avoid blocking callers.
		return Task.Run(() => GetXmlText(useCache), cancellationToken);
	}

	/// <summary>
	/// Load an XmlScreen from a Stream asynchronously.
	/// </summary>
	public async Task<Screen> LoadAsync(Stream sr, CancellationToken cancellationToken = default)
	{
		if (sr == null) throw new ArgumentNullException(nameof(sr));

		using var reader = new StreamReader(sr, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
		var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
		return await LoadFromStringAsync(text, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Load an XmlScreen from a file asynchronously.
	/// Uses FileStream with asynchronous options and BOM-aware StreamReader.
	/// </summary>
	public async Task<Screen> LoadAsync(string filename, CancellationToken cancellationToken = default)
	{
		if (filename == null) throw new ArgumentNullException(nameof(filename));
		using var fs = new FileStream(
			filename,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: 4096,
			options: FileOptions.Asynchronous | FileOptions.SequentialScan);
		using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
		var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
		return await LoadFromStringAsync(text, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Load an XmlScreen from a string asynchronously.
	/// </summary>
	public Task<Screen> LoadFromStringAsync(string text, CancellationToken cancellationToken = default)
	{
		// XmlSerializer doesn't offer async; offload deserialize to worker thread.
		return Task.Run(() => LoadFromString(text), cancellationToken);
	}

	/// <summary>
	/// Save this XmlScreen to a file asynchronously.
	/// Uses FileStream with asynchronous options and Unicode encoding to match sync Save().
	/// </summary>
	public async Task SaveAsync(string filename, CancellationToken cancellationToken = default)
	{
		if (filename == null) throw new ArgumentNullException(nameof(filename));
		var xml = await GetXmlTextAsync(useCache: false, cancellationToken).ConfigureAwait(false);
		await using var fs = new FileStream(
			filename,
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			bufferSize: 4096,
			options: FileOptions.Asynchronous | FileOptions.SequentialScan);
		await using var writer = new StreamWriter(fs, Encoding.Unicode, bufferSize: 1024, leaveOpen: false);
		await writer.WriteAsync(xml.AsMemory(), cancellationToken).ConfigureAwait(false);
		await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
	}
}
