namespace BookLibraryAPI.DTOs
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }
        public string Rating { get; set; }
        public string Availability { get; set; }
        public string DetailPageUrl { get; set; }
        public string CategoryName { get; set; }
    }
}
