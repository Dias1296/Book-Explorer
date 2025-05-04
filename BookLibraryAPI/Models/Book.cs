namespace BookLibraryAPI.Models
{
    public class Book
    {
        public int Id { get; set; }                 //Primary key
        public string Title { get; set; }           //Title of the book
        public string Price { get; set; }           //Price as a string
        public string Rating { get; set; }          //Book rating
        public string Availability { get; set; }    //Availability string (e.g. "In stock")
        public string DetailPageUrl { get; set; }   //URL to the detail page for later scraping or enrichment
        public int CategoryId { get; set; }         //Foreign key
        public Category Category { get; set; }      //Navigation property
    }
}
