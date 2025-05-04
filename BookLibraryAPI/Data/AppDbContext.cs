using Microsoft.EntityFrameworkCore;
using BookLibraryAPI.Models;

namespace BookLibraryAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}
