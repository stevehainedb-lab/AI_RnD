using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Open3270
{
	[XmlRoot("XMLScreen")]
	public partial interface IScreen
	{
		string GetText(
			uint areaStartRow,
			uint? areaEndRow,
			uint areaStartCol,
			uint? areaEndCol);

		Task<string> GetTextAsync(
			uint areaStartRow,
			uint? areaEndRow,
			uint areaStartCol,
			uint? areaEndCol,
			CancellationToken cancellationToken = default);
	}
}

namespace Open3270.TN3270
{
	[XmlRoot("XMLScreen")]
	public partial class Screen
	{
		public string GetText() => string.Join(Environment.NewLine, _mScreenRows);
		
		public string GetText(
			uint areaStartRow,
			uint? areaEndRow,
			uint areaStartCol,
			uint? areaEndCol)
		{
			try
			{
				if (areaStartCol == 0 && areaStartRow == 0 && areaEndCol == null && areaEndRow == null)
				{
					//Area is the whole screen
					return GetText();
				}

				var endColumn = areaEndCol ?? (uint)Cx - 1;
				var endRow = areaEndRow ?? (uint)Cy - 1;

				var result = string.Empty;
				for (var row = areaStartRow; row <= endRow; row++)
				{
					result += string.Concat(GetRow((int)row).AsSpan((int)areaStartCol, (int)(endColumn - areaStartCol)), Environment.NewLine);
				}

				return result;
			}
			catch (Exception e)
			{
				Console.WriteLine("Error getting text: " + e);
				throw;
			}
		}

		public Task<string> GetTextAsync(
			uint areaStartRow,
			uint? areaEndRow,
			uint areaStartCol,
			uint? areaEndCol,
			CancellationToken cancellationToken = default)
		{
			// No I/O here; offload to thread to avoid blocking UI when large buffers are processed.
			return Task.Run(() => GetText(areaStartRow, areaEndRow, areaStartCol, areaEndCol), cancellationToken);
		}
	}
}
