using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Axl.Base.Interfaces;

namespace Axl.Base.Compression.Native
{
    /// <summary>
    /// Native compression service using System.IO.Compression.
    /// Targeted at .NET 4.5.2+ with zero external dependencies.
    /// </summary>
    public class NativeCompressionService : ICompressionService
    {
        private readonly ILog _log;

        public NativeCompressionService(ILog log = null)
        {
            _log = log;
        }

        public void Compress(IEnumerable<string> files, string outputPath, CompressionFormat format, bool deleteOriginals = false, string password = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(password))
                {
                    _log?.Warn("Native Compression: Password provided but native System.IO.Compression does not support passwords. Ignoring password.");
                }

                if (format == CompressionFormat.GZip)
                {
                    var sourceFile = files.FirstOrDefault(File.Exists);
                    if (sourceFile == null) return;

                    using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open))
                    using (FileStream targetStream = File.Create(outputPath))
                    using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                    {
                        sourceStream.CopyTo(compressionStream);
                    }
                }
                else
                {
                    // For Zip, Tar, Tgz, SevenZip -> Use native ZipArchive
                    if (File.Exists(outputPath)) File.Delete(outputPath);

                    using (FileStream zipToOpen = new FileStream(outputPath, FileMode.Create))
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        foreach (var file in files.Where(File.Exists))
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                        }
                    }
                }

                if (deleteOriginals)
                {
                    foreach (var file in files.Where(File.Exists))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Native Compression Error: {ex.Message}");
                throw;
            }
        }

        public List<string> Decompress(string archivePath, string destinationDirectory, string password = null)
        {
            var extractedFiles = new List<string>();
            try
            {
                if (!File.Exists(archivePath))
                    throw new FileNotFoundException("Archive not found", archivePath);

                Directory.CreateDirectory(destinationDirectory);

                // Check if it's likely a GZip (single file) or Zip (multiple files)
                try
                {
                    using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string targetPath = Path.Combine(destinationDirectory, entry.FullName);
                            entry.ExtractToFile(targetPath, true);
                            extractedFiles.Add(targetPath);
                        }
                        return extractedFiles;
                    }
                }
                catch
                {
                    // If Zip fails, try GZip
                    string fileName = Path.GetFileNameWithoutExtension(archivePath);
                    if (!fileName.Contains(".")) fileName += ".txt";
                    string targetPath = Path.Combine(destinationDirectory, fileName);

                    using (FileStream sourceStream = new FileStream(archivePath, FileMode.Open))
                    using (FileStream targetStream = File.Create(targetPath))
                    using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(targetStream);
                    }
                    extractedFiles.Add(targetPath);
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Native Decompression Error: {ex.Message}");
                throw;
            }

            return extractedFiles;
        }
    }
}
