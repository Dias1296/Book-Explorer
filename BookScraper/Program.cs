using System;
using System.Threading.Tasks;
using BookScraper.Models;
using BookScraper.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; 

Console.WriteLine("Starting full book scraping...");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var scraperSettings = config.GetSection("ScraperSettings");
/*string baseUrl = scraperSettings["BaseUrl"];
int maxRetries = int.Parse(scraperSettings["MaxRetires"]);
int delayBetweenRequests = int.Parse(scraperSettings["DelayBetweenRequestsMs"]);*/

using (var context = new BookContext())
{
    var scraper = new BookScraper.BookScraper();

    var newCategories = await scraper.ScrapeCategoriesAsync(context, scraperSettings);
    //Save scraped categories to the database
    context.Categories.AddRange(newCategories);
    await context.SaveChangesAsync();

    var newBooks = await scraper.ScrapeBooksAsync(context, scraperSettings);
    //Save scraped books to the database
    context.Books.AddRange(newBooks);
    await context.SaveChangesAsync();

    Console.WriteLine($"Scraping complete. {newBooks.Count} books saved to database.");

    //await scraper.FixEncodedTitlesAsync(context);
}


