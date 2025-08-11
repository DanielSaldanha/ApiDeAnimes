using ApiDeAnimes.Data;
using ApiDeAnimes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace ApiDeAnimes.Controllers
{
    public class AnimeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _Rcache;

        public AnimeController(AppDbContext context, Microsoft.Extensions.Caching.Distributed.IDistributedCache Rcache)
        {
            _context = context;
            _Rcache = Rcache;
        }

        [HttpGet("buscar animes")]
        public async Task<ActionResult> BuscarAnime()
        {
            var cache = await _Rcache.GetAsync("Animes");
            if (cache != null)
            {
                var dataString = Encoding.UTF8.GetString(cache);
                var animes = JsonSerializer.Deserialize<List<Anime>>(dataString);
                return Ok(animes);
            }

            var animesList = _context.Animes.ToList();
            var serializedAnimes = JsonSerializer.Serialize(animesList);
            await _Rcache.SetAsync("Animes", Encoding.UTF8.GetBytes(serializedAnimes),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });

            return Ok(animesList);
        }

        [HttpGet("buscar um anime")]
        public async Task<ActionResult> BuscarAnimePorId(int id)
        {
            var cache = await _Rcache.GetAsync($"Anime_{id}");
            if (cache != null)
            {
                var dataString = Encoding.UTF8.GetString(cache);
                var anime = JsonSerializer.Deserialize<Anime>(dataString);
                return Ok(anime);
            }

            var animeFromDb = await _context.Animes.FindAsync(id);
            if (animeFromDb == null)
            {
                return NotFound();
            }

            var serializedAnime = JsonSerializer.Serialize(animeFromDb);
            await _Rcache.SetAsync($"Anime_{id}", Encoding.UTF8.GetBytes(serializedAnime),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });

            return Ok(animeFromDb);
        }

        [HttpGet("buscar um anime sem redis")]
        public async Task<ActionResult> BuscarAnimePorIdSemRedis(int id)
        {
            var anime = await _context.Animes.FindAsync(id);
            return Ok(anime);
        }
    }
}