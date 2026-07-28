using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Axl.Base.Interfaces;
using Axl.Base.Models;
using Axl.Base.Historian.WW.Interfaces;
using Axl.Base.Historian.WW.Models;
using Axl.Base.Historian.WW.Services;
using Moq;
using NUnit.Framework;

namespace Axl.Base.Tests
{
    [TestFixture]
    public class HistorianWWTests
    {
        private Mock<ISql> _dbMock;
        private IWWHistorian _historian;

        [SetUp]
        public void SetUp()
        {
            _dbMock = new Mock<ISql>();
            _historian = new WWHistorianService(_dbMock.Object);
        }

        [Test]
        public async Task GetLiveValuesAsync_ShouldCallDbAndReturnData()
        {
            // Arrange
            var tags = new List<string> { "Tag1", "Tag2" };
            var dbResult = Result<List<ValTag>>.Success(new List<ValTag>
            {
                new ValTag { Tag = "Tag1", Valor = 10.5 },
                new ValTag { Tag = "Tag2", Valor = 20.0 }
            });

            _dbMock.Setup(x => x.GetList<ValTag>(It.IsAny<string>(), It.IsAny<object>()))
                   .ReturnsAsync(dbResult);

            // Act
            var result = await _historian.GetLiveValuesAsync(tags);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Value.Count);
            Assert.AreEqual(10.5, result.Value[0].Valor);
            _dbMock.Verify(x => x.GetList<ValTag>(It.Is<string>(s => s.Contains("v_AnalogLive")), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task GetHealthStatusAsync_ShouldFilterByQuality()
        {
            // Arrange
            var dbResult = Result<List<WWResponse>>.Success(new List<WWResponse>
            {
                new WWResponse { Tag = "BadTag" }
            });

            _dbMock.Setup(x => x.GetList<WWResponse>(It.IsAny<string>(), It.IsAny<object>()))
                   .ReturnsAsync(dbResult);

            // Act
            var result = await _historian.GetHealthStatusAsync(true);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            _dbMock.Verify(x => x.GetList<WWResponse>(It.Is<string>(s => s.Contains("Quality <> 192")), It.IsAny<object>()), Times.Once);
        }

        [Test]
        public async Task UpdateTagsDataAsync_ShouldMapResultsToTags()
        {
            // Arrange
            var tags = new List<SupTag>
            {
                new SupTag { Tag = "T1" },
                new SupTag { Tag = "T2" }
            };

            var dbResult = Result<List<ValTag>>.Success(new List<ValTag>
            {
                new ValTag { Tag = "T1", Valor = 100 }
            });

            _dbMock.Setup(x => x.GetList<ValTag>(It.IsAny<string>(), It.IsAny<object>()))
                   .ReturnsAsync(dbResult);

            // Act
            var result = await _historian.UpdateTagsDataAsync(tags);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(tags[0].Resultado);
            Assert.AreEqual(100, tags[0].Resultado.Valor);
            Assert.IsNull(tags[1].Resultado);
        }

        [Test]
        public async Task GetLiveValuesAsync_WithLargeTagList_ShouldChunkRequests()
        {
            // Arrange
            var historianWithSmallChunk = new WWHistorianService(_dbMock.Object, 2);
            var tags = new List<string> { "T1", "T2", "T3", "T4", "T5" }; // 3 chunks (2, 2, 1)

            _dbMock.Setup(x => x.GetList<ValTag>(It.IsAny<string>(), It.IsAny<object>()))
                   .ReturnsAsync(Result<List<ValTag>>.Success(new List<ValTag>()));

            // Act
            await historianWithSmallChunk.GetLiveValuesAsync(tags);

            // Assert
            _dbMock.Verify(x => x.GetList<ValTag>(It.IsAny<string>(), It.IsAny<object>()), Times.Exactly(3));
        }
    }
}
