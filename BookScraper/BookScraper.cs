using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

using HtmlAgilityPack;
using BookScraper.Models;
using BookScraper.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace BookScraper
{
    class BookScraper
    {
        private const string baseUrl = "https://books.toscrape.com/catalogue/";
        private const string pageUrlPattern = "page-{0}.html";

        public async Task<List<Book>> ScrapeBooksAsync(BookContext context, IConfiguration scraperSettings)
        {
            List<Book> books = new List<Book>();
            List<string> failedBooks = new List<string>();
            HttpClient httpClient = new HttpClient();

            for (int page = 1; page <= 50; page++)
            {
                //Construct the book page
                string pageUrl = new Uri(new Uri(baseUrl), string.Format(pageUrlPattern, page)).ToString();

                Console.WriteLine($"Scraping {pageUrl}");

                //Try to load the website HTML using HttpClient
                string? html;
                try
                {
                    html = await httpClient.GetStringAsync(pageUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch page {page}: {ex.Message}");
                    continue;
                }
                

                //Load HTML into HtmlAgilityPack parser
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                //Select all book containers on the page
                var bookNodes = htmlDoc.DocumentNode.SelectNodes("//article[@class='product_pod']");
                if (bookNodes == null) continue;

                foreach (var node in bookNodes)
                {
                    //Extract the title
                    var title = node.SelectSingleNode(".//h3/a").GetAttributeValue("title", "N/A");
                    title = WebUtility.HtmlDecode(title);

                    //Extract the price
                    var price = node.SelectSingleNode(".//p[@class='price_color']")?.InnerText.Trim() ?? "N/A";

                    //Extract the rating
                    var ratingClass = node.SelectSingleNode(".//p[contains(@class, 'star-rating')]")?
                            .GetAttributeValue("class", "star-rating Zero");
                    var rating = ratingClass?.Split(' ')[1];

                    //Extract availability
                    var availability = node.SelectSingleNode(".//p[@class='instock availability']")?
                            .InnerText.Trim();

                    //Extract relative link to book detail page
                    var relativeLink = node.SelectSingleNode(".//h3/a").GetAttributeValue("href", "");
                    var normalizedLink = relativeLink.Replace("../", "");
                    var absoluteUrl = new Uri(new Uri(baseUrl), normalizedLink).ToString();

                    //Prevent duplicate insertion
                    bool exists = context.Books.Any(b => b.DetailPageUrl == absoluteUrl);
                    if (exists)
                    {
                        Console.WriteLine($"Skipping duplicate: {title} \n");
                        continue;
                    }

                    //Get breadcrumb that associates book to category
                    string? detailHtml;
                    HtmlDocument? detailDoc = new HtmlDocument();
                    try
                    {
                        detailHtml = await httpClient.GetStringAsync(absoluteUrl);
                        detailDoc = new HtmlDocument();
                        detailDoc.LoadHtml(detailHtml);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scraping book at {absoluteUrl}: {ex.Message}");
                        failedBooks.Add(absoluteUrl);
                        continue;
                    }
                    
                    var breadcrumbNodes = detailDoc.DocumentNode.SelectNodes("//ul[@class='breadcrumb']/li");
                    var categoryName = breadcrumbNodes[2].InnerText.Trim();

                    if (breadcrumbNodes != null && breadcrumbNodes.Count >= 3)
                    {
                        Console.Write($"Book '{title}' is in category '{categoryName}'.");
                    }
                    else
                    {
                        Console.WriteLine($"[Warning] Failed to extract category for: {title} | {absoluteUrl}");
                        continue;
                    }

                    //Try to get the category from the DB
                    var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                    if (category == null)
                    {
                        Console.WriteLine($"[Warning] Category '{categoryName}' not found. Skipping book '{title}'.");
                        continue; //Skip books with unknown categories
                    }

                    books.Add(new Book
                    {
                        Title = title,
                        Price = price,
                        Rating = rating,
                        Availability = availability,
                        DetailPageUrl = absoluteUrl,
                        CategoryId = category.Id,
                    });

                    Console.WriteLine($"Inserted book '{title}' into database.");
                }

            }

            //Add failed books to file
            if (failedBooks.Any())
            {
                string filePath = "failed_books.txt";
                File.WriteAllLines(filePath, failedBooks);
                Console.WriteLine($"Saved {failedBooks.Count} failed book URLs to {filePath}");
            }
            else
            {
                Console.WriteLine("No failed books to log.");
            }

            return books;
        }

        public async Task<List<Category>> ScrapeCategoriesAsync(BookContext context, IConfiguration scraperSettings)
        {
            var categoryList = new List<Category>();
            var categories = new List<(string, string)>();
            var baseUrl = scraperSettings["BaseUrl"];

            var httpClient = new HttpClient();
            string? response; 
            try
            {
                response = await httpClient.GetStringAsync(baseUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch category page: {ex.Message}");
                //Return an empty response
                return new List<Category>();
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            //Select category links from the sidebar
            var nodes = htmlDoc.DocumentNode.SelectNodes("//ul[@class='nav nav-list']/li/ul/li/a");

            foreach (var node in nodes)
            {
                var name = node.InnerText.Trim();
                var href = node.GetAttributeValue("href", "").Trim();

                if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(href))
                {
                    var fullUrl = new Uri(new Uri(baseUrl), href).ToString();
                    categories.Add((name, fullUrl));
                }
            }

            foreach (var (name, _) in categories)
            {
                //Check if the category already exists in the database
                var exists = await context.Categories.AnyAsync(c => c.Name == name);

                if (!exists)
                {
                    categoryList.Add(new Category { Name = name });
                    Console.WriteLine($"Adding category '{name}' to database. \n");
                }
            }

            return categoryList;
        }

        public async Task FixEncodedTitlesAsync(BookContext context)
        {
            var booksWithEncodedTitles = await context.Books.ToListAsync();

            foreach (var book in booksWithEncodedTitles)
            {
                var decodedTitle = WebUtility.HtmlDecode(book.Title);
                if (decodedTitle != book.Title)
                {
                    book.Title = decodedTitle;
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine("All encoded book titles have been fixed.");
        }

        private async Task<string?> GetHtmlWithRetry(string url, HttpClient httpClient, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await httpClient.GetStringAsync(url);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"[Attempt {attempt}] Failed to fetch {url}: {ex.Message}");
                    await Task.Delay(1000);
                }
            }

            Console.WriteLine($"Failed to fetch {url} after {maxRetries} attempts.");
            return null;
        }
    }
}
