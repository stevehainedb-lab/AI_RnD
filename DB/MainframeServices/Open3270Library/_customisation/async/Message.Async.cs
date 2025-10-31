using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Open3270.Library
{
	/// <summary>
	/// Async counterparts for Message operations. Maintains wire format compatibility with the sync APIs.
	/// </summary>
	internal partial class Message
	{
		/// <summary>
		/// Serialize this message into a byte[] payload asynchronously (offloads synchronous XmlSerializer).
		/// </summary>
		public Task<byte[]> SerializeAsync(CancellationToken cancellationToken = default)
		{
			return Task.Run(() =>
			{
				var serializer = new XmlSerializer(typeof(Message));
				using var ms = new MemoryStream();
				serializer.Serialize(ms, this);
				return ms.ToArray();
			}, cancellationToken);
		}

		/// <summary>
		/// Send this message over a Socket asynchronously, using the same header framing as Send().
		/// </summary>
		public async Task SendAsync(Socket socket, CancellationToken cancellationToken = default)
		{
			if (socket == null) throw new ArgumentNullException(nameof(socket));
			if (!socket.Connected) throw new ApplicationException("Sorry, socket is not connected");

			var payload = await SerializeAsync(cancellationToken).ConfigureAwait(false);

			// Build header
			var header = new MessageHeader { UMessageSize = payload.Length };
			var headerBytes = header.ToByte();

#if NET6_0_OR_GREATER
			await socket.SendAsync(headerBytes, SocketFlags.None, cancellationToken).ConfigureAwait(false);
			await socket.SendAsync(payload, SocketFlags.None, cancellationToken).ConfigureAwait(false);
#else
			// Fallback without cancellation token
			await socket.SendAsync(new ArraySegment<byte>(headerBytes), SocketFlags.None).ConfigureAwait(false);
			await socket.SendAsync(new ArraySegment<byte>(payload), SocketFlags.None).ConfigureAwait(false);
#endif
		}

		/// <summary>
		/// Deserialize a Message from a byte[] asynchronously. Wraps the synchronous CreateFromByteArray.
		/// </summary>
		public static Task<Message> CreateFromByteArrayAsync(byte[] data, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => CreateFromByteArray(data), cancellationToken);
		}
	}
}
