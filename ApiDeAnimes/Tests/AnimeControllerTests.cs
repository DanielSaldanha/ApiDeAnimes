using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using ApiDeAnimes.Controllers;
using ApiDeAnimes.Models;
using ApiDeAnimes.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;

namespace ApiDeAnimes.Tests
{
    [TestFixture]
    public class AnimeControllerTests
    {
        private Mock<IDistributedCache> _mockCache;
        private AppDbContext _dbContext;
        private AnimeController _controller;

        [SetUp]
        public void Setup()
        {
            _mockCache = new Mock<IDistributedCache>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _dbContext = new AppDbContext(options);
            _controller = new AnimeController(_dbContext, _mockCache.Object);
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

            _dbContext.Animes.Add(new Anime { Id = 1, anime = "Naruto" });
            _dbContext.SaveChanges();

            // Act
            var result = await _controller.BuscarAnime();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var animes = okResult.Value as List<Anime>;
            Assert.IsNotNull(animes);
          //  Assert.Single(animes);
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

            _dbContext.Animes.Add(new Anime { Id = 1, anime = "Naruto" });
            _dbContext.SaveChanges();

            // Act
            var result = await _controller.BuscarAnimePorId(1);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var anime = okResult.Value as Anime;
            Assert.IsNotNull(anime);
            Assert.AreEqual("Naruto", anime.anime);

            // Verifica se o anime foi armazenado no cache
            _mockCache.Verify(x => x.SetAsync("Anime_1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task BuscarAnimePorIdSemRedis_ShouldReturnAnime()
        {
            // Arrange
            _dbContext.Animes.Add(new Anime { Id = 1, anime = "Naruto" });
            _dbContext.SaveChanges();

            // Act
            var result = await _controller.BuscarAnimePorIdSemRedis(1);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var anime = okResult.Value as Anime;
            Assert.IsNotNull(anime);
            Assert.AreEqual("Naruto", anime.anime);
        }
    }
}