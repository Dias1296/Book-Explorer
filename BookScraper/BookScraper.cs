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

namespace BookScraper
{
    class BookScraper
    {
        private const string baseUrl = "https://books.toscrape.com/catalogue/";
        private const string pageUrlPattern = "page-{0}.html";

        public async Task<List<Book>> ScrapeBooksAsync(BookContext context)
        {
            List<Book> books = new List<Book>();
            HttpClient httpClient = new HttpClient();

            for (int page = 1; page <= 50; page++)
            {
                //Construct the book page
                string pageUrl = new Uri(new Uri(baseUrl), string.Format(pageUrlPattern, page)).ToString();

                Console.WriteLine($"Scraping {pageUrl}");

                //Load the website HTML using HttpClient
                var html = await httpClient.GetStringAsync(pageUrl);

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
                    var detailHtml = await httpClient.GetStringAsync(absoluteUrl);
                    var detailDoc = new HtmlDocument();
                    detailDoc.LoadHtml(detailHtml);

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

                    //Console.WriteLine($"Added book: {title}");
                }

            }

            return books;
        }

        public async Task<List<(string Name, string Url)>> ScrapeCategoriesAsync()
        {
            var categories = new List<(string, string)>();
            var baseUrl = "https://books.toscrape.com/";

            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(baseUrl);

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

            return categories;
        }
    }
}
