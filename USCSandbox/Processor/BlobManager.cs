using AssetRipper.Primitives;
using AssetsTools.NET;

namespace USCSandbox.Processor
{
    public class BlobManager
    {
        private List<byte[]> _segments;
        private UnityVersion _engVer;

        public List<BlobEntry> Entries;

        // The entry table is at the start of the first segment; each entry points into its own segment.
        public BlobManager(List<byte[]> segments, UnityVersion engVer)
        {
            _segments = segments;
            _engVer = engVer;

            var reader = new AssetsFileReader(new MemoryStream(segments[0]));
            var count = reader.ReadInt32();
            Entries = new List<BlobEntry>(count);
            for (var i = 0; i < count; i++)
            {
                Entries.Add(new BlobEntry(reader, engVer));
            }
        }

        public byte[] GetRawEntry(int index)
        {
            var entry = Entries[index];
            return _segments[entry.Segment].AsSpan(entry.Offset, entry.Length).ToArray();
        }

        public ShaderParams GetShaderParams(int index)
        {
            var blobEntry = GetRawEntry(index);
            var r = new AssetsFileReader(new MemoryStream(blobEntry));
            return new ShaderParams(r, _engVer, true);
        }

        public ShaderSubProgram GetShaderSubProgram(int index)
        {
            var blobEntry = GetRawEntry(index);
            var r = new AssetsFileReader(new MemoryStream(blobEntry));
            return new ShaderSubProgram(r, _engVer);
        }
    }
}
