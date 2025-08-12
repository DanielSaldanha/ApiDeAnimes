using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using ApiDeAnimes.Controllers;
using ApiDeAnimes.Models;
using ApiDeAnimes.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiDeAnimes.Tests
{
    [TestFixture]
    public class AnimeControllerTests
    {
        private Mock<IDistributedCache> _mockCache;
        private Mock<AppDbContext> _mockContext;
        private Mock<DbSet<Anime>> _mockSet;
        private AnimeController _controller;

        [SetUp]
        public void Setup()
        {
            _mockCache = new Mock<IDistributedCache>();
            _mockContext = new Mock<AppDbContext>();
            _mockSet = new Mock<DbSet<Anime>>();

            _controller = new AnimeController(_mockContext.Object, _mockCache.Object);
        }

        private void SetupMockData(List<Anime> data)
        {
            var queryableData = data.AsQueryable();
            _mockSet.As<IQueryable<Anime>>().Setup(m => m.Provider).Returns(queryableData.Provider);
            _mockSet.As<IQueryable<Anime>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            _mockSet.As<IQueryable<Anime>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            _mockSet.As<IQueryable<Anime>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());

            _mockContext.Setup(c => c.Animes).Returns(_mockSet.Object);

            // Para FindAsync, você pode configurar o mock para retornar o item correto
            _mockContext.Setup(c => c.Animes.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => data.Find(a => a.Id == (int)ids[0]));
        }

        [Test]
        public async Task BuscarAnime_ShouldReturnCachedValue_WhenCacheExists()
        {
            // Arrange
            var cachedAnimes = JsonSerializer.Serialize(new List<Anime>
            {
                new Anime { Id = 1, anime = "Naruto" }
            });
            var cachedData = Encoding.UTF8.GetBytes(cachedAnimes);
            _mockCache.Setup(x => x.GetAsync("Animes", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _controller.BuscarAnime();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var animes = okResult.Value as List<Anime>;
            Assert.IsNotNull(animes);
           // Assert.Single(animes);
            Assert.AreEqual("Naruto", animes[0].anime);
        }

        [Test]
        public async Task BuscarAnime_ShouldReturnAnimes_WhenCacheDoesNotExist()
        {
            // Arrange
            _mockCache.Setup(x => x.GetAsync("Animes", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            var animesList = new List<Anime>
            {
                new Anime { Id = 1, anime = "Naruto" }
            };

            // Configure mock data
            SetupMockData(animesList);

            // Act
            var result = await _controller.BuscarAnime();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var animes = okResult.Value as List<Anime>;
            Assert.IsNotNull(animes);
           // Assert.Single(animes);
            Assert.AreEqual("Naruto", animes[0].anime);

            // Verifica se os dados foram armazenados no cache
            _mockCache.Verify(x => x.SetAsync("Animes", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task BuscarAnimePorId_ShouldReturnCachedValue_WhenCacheExists()
        {
            // Arrange
            var cachedAnime = JsonSerializer.Serialize(new Anime { Id = 1, anime = "Naruto" });
            var cachedData = Encoding.UTF8.GetBytes(cachedAnime);
            _mockCache.Setup(x => x.GetAsync("Anime_1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedData);

            // Act
            var result = await _controller.BuscarAnimePorId(1);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var anime = okResult.Value as Anime;
            Assert.IsNotNull(anime);
            Assert.AreEqual("Naruto", anime.anime);
        }

        [Test]
        public async Task BuscarAnimePorId_ShouldReturnAnime_WhenCacheDoesNotExist()
        {
            // Arrange
            _mockCache.Setup(x => x.GetAsync("Anime_1", It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            var animesList = new List<Anime>
            {
                new Anime { Id = 1, anime = "Naruto" }
            };

            // Use the setup method to configure mock data
            SetupMockData(animesList);

            _mockContext.Setup(c => c.Animes.FindAsync(It.IsAny<object[]>()))
.ReturnsAsync((object[] ids) => animesList.Find(a => a.Id == (int)ids[0]));

            // Act
            var result = await _controller.BuscarAnimePorId(1);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var animeResult = okResult.Value as Anime;
            Assert.IsNotNull(animeResult);
            Assert.AreEqual("Naruto", animeResult.anime);

            // Verifica se o anime foi armazenado no cache
            _mockCache.Verify(x => x.SetAsync("Anime_1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}