using ApiDeAnimes.Data;
using ApiDeAnimes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ApiDeAnimes.Controllers
{
    public class AnimeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _Rcache;

        public AnimeController(AppDbContext context, IDistributedCache Rcache)
        {
            _context = context;
            _Rcache = Rcache;
        }

        [HttpGet("buscar animes")]
        public async Task<ActionResult> BuscarAnime()
        {
            var cache = await _Rcache.GetStringAsync("Animes");
            if(cache != null)
            {
                return Ok(JsonSerializer.Deserialize<List<Anime>>(cache));
            }

            var animes = _context.Animes.ToList();
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
            await _Rcache.SetStringAsync("Animes", JsonSerializer.Serialize(animes),options);
            return Ok(animes);
        }

        [HttpGet("buscar um anime")]
        public async Task<ActionResult> BuscarAnimePorId(int id)
        {
            var cache = await _Rcache.GetStringAsync($"Anime_{id}");
            if (cache != null)
            {
                return Ok(JsonSerializer.Deserialize<Anime>(cache));
            }
            var anime = await _context.Animes.FindAsync(id);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            await _Rcache.SetStringAsync("Animes", JsonSerializer.Serialize(anime),options);
            return Ok(anime);
        }
        [HttpGet("buscar um anime sem redis")]
        public async Task<ActionResult> BuscarAnimePorIdSemRedis(int id)
        {
            var anime = await _context.Animes.FindAsync(id);
            return Ok(anime);
        }
    }
}
