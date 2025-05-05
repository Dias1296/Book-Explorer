namespace BookLibraryAPI.DTOs
{
    public class BookQueryParameters
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Rating { get; set; }
        public bool? Available { get; set; }

        //Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        //Sorting
        public SortField? SortBy { get; set; }
        public bool Descending { get; set; } = false;
    }

    public enum SortField
    {
        Title,
        Price,
        Rating,
        CategoryName
    }
}
