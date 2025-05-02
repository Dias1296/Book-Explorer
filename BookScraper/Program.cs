using System;
using System.Threading.Tasks;
using BookScraper.Models;
using BookScraper.DatabaseContext;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("Starting full book scraping...");




using (var context = new BookContext())
{
    var scraper = new BookScraper.BookScraper();

    var categories = await scraper.ScrapeCategoriesAsync();

    foreach (var (name, _) in categories)
    {
        //Check if the category already exists in the database
        var exists = await context.Categories.AnyAsync(c => c.Name == name);

        if (!exists)
        {
            context.Categories.Add(new Category { Name = name });
            //Console.WriteLine($"Added category: {name} \n");
        }
    }

    await context.SaveChangesAsync();

    var newBooks = await scraper.ScrapeBooksAsync(context);

    //Save scraped books to the database
    context.Books.AddRange(newBooks);
    await context.SaveChangesAsync();

    Console.WriteLine($"Scraping complete. {newBooks.Count} books saved to database.");


}


