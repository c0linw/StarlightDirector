using System.IO;

namespace OpenCGSS.StarlightDirector.Previewing.Audio.Extensions {
    internal static class StreamExtensions {

        internal static byte[] ReadToEnd(this Stream stream) {
            return ReadToEnd(stream, 102400);
        }

        internal static byte[] ReadToEnd(this Stream stream, int bufferSize) {
            byte[] data;
            var buffer = new byte[bufferSize];
            var length = stream.Length;

            using (var memoryStream = new MemoryStream()) {
                var read = 1;
                long totalRead = 0;

                while (read > 0) {
                    read = stream.Read(buffer, 0, bufferSize);
                    memoryStream.Write(buffer, 0, read);

                    totalRead += read;

                    // totalread >= length: for WaveOffsetStream
                    if (read < bufferSize || totalRead >= length) {
                        break;
                    }
                }

                data = memoryStream.ToArray();
            }

            return data;
        }

    }
}
