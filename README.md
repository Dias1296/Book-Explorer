# Book-Explorer
Book Explorer App - C# .NET (backend) + React (frontend) + SQL Server (database)

	The idea behind this app is to use the information from the https://books.toscrape.com/ website
and build from that a website that that uses multiple technologies to enrich and visualize the book data from the aforementioned source.
That includes:
	- Web scraping (data acquisition)
	- Backend/API design (serving data)
	- Data enrichment (pulling more info from other sources)
	- Data visualization (charts/UI)
	
The main technologies used to accomplish this will be .NET for the backend, React for the frontend and SQL Server for the database.

Feature incrementation (commit) log:
1. 	Built the initial web scraper that scrapes info from each book in the https://books.toscrape.com/ website. 
That includes info such as title, price, category...
2. 	Fixed a problem with book titles being scraped with HTML encoding.
	Added some robustness to the HTML requests.
	Added some console logs.
	Added systems to save failed books URLs in a text file.
3.	Started the backend development. Basic infrastructure setup with CRUD endpoints to access the original scraped data.
	

