using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookScraper.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        //One category can have many books.
        public ICollection<Book> Books { get; set; }
    }
}
