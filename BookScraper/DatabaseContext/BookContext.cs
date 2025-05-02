using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace BookScraper.DatabaseContext
{
    class BookContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Category> Categories { get; set; }

        //Configure the database connection
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=BookScraperDb;Trusted_Connection=True;");
        }
    }
}
