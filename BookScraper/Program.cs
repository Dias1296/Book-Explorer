using System;
using System.Threading.Tasks;
using BookScraper.Models;
using BookScraper.DatabaseContext;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Starting full book scraping...");




using (var context = new BookContext())
{
    var scraper = new BookScraper.BookScraper();

    var newCategories = await scraper.ScrapeCategoriesAsync(context);
    //Save scraped categories to the database
    context.Categories.AddRange(newCategories);
    await context.SaveChangesAsync();

    var newBooks = await scraper.ScrapeBooksAsync(context);
    //Save scraped books to the database
    context.Books.AddRange(newBooks);
    await context.SaveChangesAsync();

    Console.WriteLine($"Scraping complete. {newBooks.Count} books saved to database.");

    //await scraper.FixEncodedTitlesAsync(context);
}


