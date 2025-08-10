using ApiDeAnimes.Controllers;
using ApiDeAnimes.Data;
using ApiDeAnimes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiDeAnimes.Tests
{
    public class AnimeControllerTest
    {
        private AnimeController _controller;
        private Mock<DbSet<Anime>> _mockSet;
        private Mock<AppDbContext> _mockContext;
        private Mock<IDistributedCache> _mockCache;

        [SetUp]
        public void Setup()
        {
            _mockSet = new Mock<DbSet<Anime>>();
            _mockContext = new Mock<AppDbContext>();
            _mockCache = new Mock<IDistributedCache>();
            _controller = new AnimeController(_mockContext.Object, _mockCache.Object);
        }

        private void SetupMockData(List<Anime> data)
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<Anime>>();

            mockSet.As<IQueryable<Anime>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            mockSet.As<IQueryable<Anime>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<Anime>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<Anime>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());

            _mockContext.Setup(c => c.Animes).Returns(_mockSet.Object);
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TesteDb")
            .Options;

            _mockContext = new Mock<AppDbContext>(options);
        }

        [Test]
        public async Task BuscarAnimePorId_DeveRetornar_Ok_Com_Cache()
        {
            // Arrange
            int animeId = 1;
            var anime = new Anime { Id = animeId, anime = "Naruto" };
            var cacheKey = $"Anime_{animeId}";

            // Configurar o mock para retornar o anime em formato JSON do cache
            _mockCache.Setup(m => m.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((string key, CancellationToken token) =>
                      {
                          return key == cacheKey ? JsonSerializer.Serialize(anime) : null;
                      });

            // Act
            var result = await _controller.BuscarAnimePorId(animeId);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.IsInstanceOf<Anime>(okResult.Value);
            Assert.AreEqual(anime, okResult.Value);
        }

        //[Test]
        //public async Task BuscarAnimePorId_DeveRetornar_Ok_Sem_Cache()
        //{
        //    // Arrange
        //    int animeId = 1;
        //    var anime = new Anime { Id = animeId, anime = "Naruto" };

        //    // Configurar o mock para retornar null do cache
        //    _mockCache.Setup(m => m.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        //               .ReturnsAsync((string)null);

        //    // Configurar o mock do contexto para retornar o anime
        //    _mockContext.Setup(m => m.Animes.FindAsync(animeId)).ReturnsAsync(anime);

        //    // Act
        //    var result = await _controller.BuscarAnimePorId(animeId);

        //    // Assert
        //    var okResult = result as OkObjectResult;
        //    Assert.IsNotNull(okResult);
        //    Assert.AreEqual(200, okResult.StatusCode);
        //    Assert.AreEqual(anime, okResult.Value);

        //    // Verificar se o cache foi configurado corretamente
        //    _mockCache.Verify(m => m.SetStringAsync($"Anime_{animeId}", JsonSerializer.Serialize(anime),
        //        It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        //}
        [Test]
        public async Task BuscarAnimePorIdSemRedis_DeveRetornar_Ok()
        {
            var ListaDeAnimes = new List<Anime>
            {
                new Anime {
                Id =1,
                anime = "naruto"
                }
            };
            SetupMockData(ListaDeAnimes);

            //mockagem do FindAsync utilizado para a obtenção do id do dado desejado
            _mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync((object[] ids) =>
            {
                return ListaDeAnimes.FirstOrDefault(i => i.Id == (int)ids[0]);
            });

            // Act
            var result = await _controller.BuscarAnimePorIdSemRedis(1);

            // Assert
            var OkRes = result as OkObjectResult;
            Assert.IsNotNull(OkRes);
            Assert.AreEqual(200, OkRes.StatusCode);

            Assert.AreEqual("naruto", result.Id);
        }
    }
}