using Microsoft.EntityFrameworkCore;
using ApiDeAnimes.Models;
namespace ApiDeAnimes.Data
{
    public class AppDbContext : DbContext
    {
        // Construtor padrão
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public virtual DbSet<Anime> Animes { get; set; }
    }
}
