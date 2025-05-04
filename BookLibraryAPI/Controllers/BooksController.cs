using BookLibraryAPI.Data;
using BookLibraryAPI.DTOs;
using BookLibraryAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookLibraryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        //GET request to get all books 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks()
        {
            var books = await _context.Books
                .OrderBy(b => b.Id)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Price = b.Price,
                    Rating = b.Rating,
                    Availability = b.Availability,
                    DetailPageUrl = b.DetailPageUrl,
                    CategoryName = b.Category.Name
                })
                .ToListAsync();

            return Ok(books);
        }

        //GET request to get books by id
        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBookById(int id)
        {
            var book = await _context.Books
                .Where(b => b.Id == id)
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Price = b.Price,
                    Rating = b.Rating,
                    Availability = b.Availability,
                    DetailPageUrl = b.DetailPageUrl,
                    CategoryName = b.Category.Name
                })
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }

            return Ok(book);
        }

        //POST request to add books
        [HttpPost]
        public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto dto)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest("Invalid category ID.");
            }

            var book = new Book
            {
                Title = dto.Title,
                Price = dto.Price,
                Rating = dto.Rating,
                Availability = dto.Availability,
                DetailPageUrl = dto.DetailPageUrl,
                Category = category
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Price = book.Price,
                Rating = book.Rating,
                Availability = book.Availability,
                DetailPageUrl = book.DetailPageUrl,
                CategoryName = category.Name
            });
        }

        //Search books by category
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks(string? category, bool? available)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(b => b.Category.Name == category);
            }

            if (available.HasValue)
            {
                query = query.Where(b => b.Availability.Contains((bool)available ? "In stock" : "Not in stock"));
            }

            var results = await query
                .Select(b => new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Price = b.Price,
                    Rating = b.Rating,
                    Availability = b.Availability,
                    DetailPageUrl = b.DetailPageUrl,
                    CategoryName = b.Category.Name
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, CreateBookDto dto)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
            {
                return BadRequest("Invalid category ID.");
            }

            book.Title = dto.Title;
            book.Price = dto.Price;
            book.Rating = dto.Rating;
            book.Availability = dto.Availability;
            book.DetailPageUrl = dto.DetailPageUrl;
            book.Category = category;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        //Delete book from database
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
