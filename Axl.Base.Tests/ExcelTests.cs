using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Axl.Base.Statics;
using Axl.Base.Models;
using Moq;
using Axl.Base.Interfaces;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class ExcelTests
    {
        private string _testFile;

        [SetUp]
        public void Setup()
        {
            _testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestChaos.xlsx");
        }

        [TearDown]
        public void Cleanup()
        {
            if (File.Exists(_testFile)) File.Delete(_testFile);
        }

        [Test]
        public void ExcelHelper_CreateChaosReport_ShouldCreateFile()
        {
            ExcelHelper.CreateChaosReport(_testFile);
            Assert.IsTrue(File.Exists(_testFile));
        }

        [Test]
        public void ExcelManager_GetSheets_ShouldReturnList()
        {
            // Primero creamos un archivo válido
            ExcelHelper.CreateChaosReport(_testFile);
            
            var mockLog = new Mock<ILog>();
            var sheets = ExcelManager.GetSheets(_testFile, mockLog.Object);

            Assert.IsNotNull(sheets);
            Assert.Greater(sheets.Count, 0);
        }

        [Test]
        public void ExcelManager_ReadFast_ShouldPopulateModels()
        {
            ExcelHelper.CreateChaosReport(_testFile);
            
            var results = ExcelManager.ReadFast(_testFile);

            Assert.IsNotNull(results);
            Assert.Greater(results.Count, 0);
            Assert.IsNotEmpty(results[0].Table);
            Assert.Greater(results[0].Columns.Count, 0);
        }
    }
}

