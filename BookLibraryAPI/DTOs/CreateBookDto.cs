namespace BookLibraryAPI.DTOs
{
    public class CreateBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }
        public string Rating { get; set; }
        public string Availability { get; set; }
        public string DetailPageUrl { get; set; }
        public int CategoryId { get; set; }
    }
}
