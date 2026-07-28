using NUnit.Framework;
using System;
using System.IO;
using Axl.Base.Omdb;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class OmdbServiceTests
    {
        [Test]
        public void Constructor_InvalidPath_ThrowsDirectoryNotFoundException()
        {
            string invalidPath = @"C:\NonExistentPath_" + Guid.NewGuid();
            Assert.Throws<DirectoryNotFoundException>(() => new OmdbService(invalidPath));
        }

        [Test]
        [Explicit("Requires Foxboro tools and ksh.exe installed")]
        public void ManualTest_OmdbSimulation()
        {
            // Note: Update path if tools are elsewhere
            var service = new OmdbService(); 

            Console.WriteLine("Probando escritura de variables OM...");
            
            service.Write("TST_BOOL", true, OmType.Bool);
            service.Write("TST_FLOAT", 123.45f, OmType.Float);
            service.Write("TST_STR", "Hello Foxboro", OmType.String);

            bool bVal = service.Read<bool>("TST_BOOL", out bool exists);
            Assert.IsTrue(exists);
            Assert.IsTrue(bVal);
            Console.WriteLine($"[OM] TST_BOOL: {bVal}");

            float fVal = service.Read<float>("TST_FLOAT", out exists);
            Assert.IsTrue(exists);
            Assert.AreEqual(123.45f, fVal, 0.01f);
            Console.WriteLine($"[OM] TST_FLOAT: {fVal}");

            string sVal = service.Read<string>("TST_STR", out exists);
            Assert.IsTrue(exists);
            Assert.AreEqual("Hello Foxboro", sVal);
            Console.WriteLine($"[OM] TST_STR: {sVal}");
        }

        [Test]
        [Explicit("Requires Foxboro tools to test Packed Long bit reading")]
        public void ManualTest_ReadBitSimulation()
        {
            var service = new OmdbService();
            string plVar = "TST_PL_VAR";
            
            // Assuming we write a known hex value 0x0000000F (bits 0,1,2,3 are 1)
            // This would normally be set through Foxboro tools or service.Write if we support PL writes
            
            bool bit0 = service.ReadBit(plVar, 0, out bool exists);
            if (exists)
            {
                Console.WriteLine($"Bit 0 of {plVar} is {bit0}");
            }
            else
            {
                Console.WriteLine($"{plVar} not found for bit testing.");
            }
        }
    }
}

