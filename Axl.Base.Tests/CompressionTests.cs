using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Axl.Base.Compression.Native;
using Axl.Base.Interfaces;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class CompressionTests
    {
        private string _testDir;
        private string _outputDir;
        private List<string> _testFiles;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "AxlCompressionTests_" + Guid.NewGuid().ToString("N"));
            _outputDir = Path.Combine(_testDir, "Output");
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(_outputDir);

            _testFiles = new List<string>();
            for (int i = 1; i <= 3; i++)
            {
                string path = Path.Combine(_testDir, $"file{i}.txt");
                File.WriteAllText(path, $"Content of file {i}");
                _testFiles.Add(path);
            }
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); } catch { }
        }



        [Test]
        public void Native_Zip_ShouldCompressAndDecompress()
        {
            var service = new NativeCompressionService();
            string archivePath = Path.Combine(_outputDir, "test_native.zip");
            string extractDir = Path.Combine(_outputDir, "Extracted_Native");

            service.Compress(_testFiles, archivePath, CompressionFormat.Zip);
            Assert.IsTrue(File.Exists(archivePath));

            var results = service.Decompress(archivePath, extractDir);
            Assert.AreEqual(3, results.Count);
            foreach (var file in results)
            {
                Assert.IsTrue(File.Exists(file));
                Assert.IsTrue(File.ReadAllText(file).StartsWith("Content of file"));
            }
        }

        [Test]
        public void Native_GZip_ShouldCompressAndDecompress()
        {
            var service = new NativeCompressionService();
            string archivePath = Path.Combine(_outputDir, "test_native.gz");
            string extractDir = Path.Combine(_outputDir, "Extracted_Native_Gzip");

            // GZip only takes the first file
            service.Compress(_testFiles.Take(1), archivePath, CompressionFormat.GZip);
            Assert.IsTrue(File.Exists(archivePath));

            var results = service.Decompress(archivePath, extractDir);
            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(File.Exists(results[0]));
            Assert.AreEqual("Content of file 1", File.ReadAllText(results[0]));
        }

        [Test]
        public void Native_EmulatedFormat_ShouldWorkAsZip()
        {
            var service = new NativeCompressionService();
            string archivePath = Path.Combine(_outputDir, "test_native.7z"); // Emulated 7z
            string extractDir = Path.Combine(_outputDir, "Extracted_Native_7z");

            service.Compress(_testFiles, archivePath, CompressionFormat.SevenZip);
            Assert.IsTrue(File.Exists(archivePath));

            var results = service.Decompress(archivePath, extractDir);
            Assert.AreEqual(3, results.Count);
        }
    }
}
