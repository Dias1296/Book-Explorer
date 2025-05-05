using BookLibraryAPI.Data;
using BookLibraryAPI.DTOs;
using BookLibraryAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static System.Reflection.Metadata.BlobBuilder;

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
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks([FromQuery] BookQueryParameters query)
        {
            var booksQuery = _context.Books
                .Include(b => b.Category).AsQueryable();

            //Apply filters
            if (!string.IsNullOrEmpty(query.Title))
                booksQuery = booksQuery.Where(b => b.Title.Contains(query.Title));

            if (!string.IsNullOrEmpty(query.Category))
                booksQuery = booksQuery.Where(b => b.Category.Name ==  query.Category);

            if (!query.Rating.IsNullOrEmpty())
            {
                booksQuery = booksQuery.Where(b => b.Rating == query.Rating);
            }

            //Total count before pagination
            var totalCount = await booksQuery.CountAsync();

            //Apply ordering and pagination
            booksQuery = booksQuery
                .OrderBy(b => b.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);

            //Project to DTO
            var books = await booksQuery.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Price = b.Price,
                Rating = b.Rating,
                Availability = b.Availability,
                DetailPageUrl = b.DetailPageUrl,
                CategoryName = b.Category.Name
            }).ToListAsync();

            var result = new PagedResult<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                HasNextPage = totalCount > query.Page * query.PageSize
            };

            return Ok(result);
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
        public async Task<ActionResult<IEnumerable<BookDto>>> SearchBooks([FromQuery] BookQueryParameters parameters)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrEmpty(parameters.Category))
            {
                query = query.Where(b => b.Category.Name == parameters.Category);
            }

            if (parameters.Available.HasValue)
            {
                query = query.Where(b => b.Availability.Contains((bool)parameters.Available ? "In stock" : "Not in stock"));
            }

            query = parameters.SortBy switch
            {
                SortField.Price => parameters.Descending ? query.OrderByDescending(b => b.Price) : query.OrderBy(b => b.Price),
                SortField.Title => parameters.Descending ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
                SortField.CategoryName => parameters.Descending ? query.OrderByDescending(b => b.Category.Name) : query.OrderBy(b => b.Category.Name),
                _ => query.OrderBy(b => b.Id) //Default fallback
            };

            //Count before pagination
            var totalCount = await query.CountAsync();

            var books = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
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

            var result = new PagedResult<BookDto>
            {
                Items = books,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                HasNextPage = totalCount > parameters.Page * parameters.PageSize
            };

            return Ok(result);
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
