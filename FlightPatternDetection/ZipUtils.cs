using System.IO.Compression;
using System.Text;

namespace FlightPatternDetection
{
    public static class ZipUtils
    {
        public static string UnzipData(byte[] zippedData)
        {
            using (var compressedStream = new MemoryStream(zippedData))
            {
                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        public static byte[] ZipData(string jsonData)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);
            byte[] zippedByteData;

            using (var uncompressedStream = new MemoryStream(jsonBytes))
            {
                using (var compressedStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
                    {
                        uncompressedStream.CopyTo(gzipStream);
                    }
                    zippedByteData = compressedStream.ToArray();
                }
            }

            return zippedByteData;
        }
    }
}
