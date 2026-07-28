using System.Collections.Generic;

namespace Axl.Base.Interfaces
{
    /// <summary>
    /// Unified interface for compression services in the Axl.Base ecosystem.
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// Compresses files into an archive.
        /// </summary>
        void Compress(IEnumerable<string> files, string outputPath, CompressionFormat format, bool deleteOriginals = false, string password = null);

        /// <summary>
        /// Decompresses an archive to a directory.
        /// </summary>
        List<string> Decompress(string archivePath, string destinationDirectory, string password = null);
    }

    /// <summary>
    /// Supported compression formats.
    /// </summary>
    public enum CompressionFormat
    {
        Zip,
        Tar,
        GZip,
        Tgz,
        SevenZip,
        Rar
    }
}
