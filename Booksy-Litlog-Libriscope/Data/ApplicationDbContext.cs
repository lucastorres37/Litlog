using Litlog.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Litlog.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Diario> Diarios { get; set; }
        public DbSet<Livro> Livros { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Log> LivrosLidos { get; set; }
    }
}
