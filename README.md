## Intro

This code can be used to analyze the Danish state budget. You can feed it Html from the [government budget database](http://www.oes-cs.dk/olapdatabase/finanslov/index.cgi). And you can make it output CSV-formatted content that you can feed to IBM Many Eyes to create visualizations of the budget like [this one](http://www-958.ibm.com/software/analytics/manyeyes/visualizations/danish-state-budget-2013-as-treema). Here's a [blog post with details](http://friism.com/danish-state-budget-data).

## How to operate the loader

* Use Firefox and visit [http://www.oes-cs.dk/olapdatabase/finanslov/index.cgi](http://www.oes-cs.dk/olapdatabase/finanslov/index.cgi)
* Click "Vælg struktur" and select all the variables.
* Select "Alle niveauer på én gang"

This will cause the site to give you a fully detailed budget for the selected year. It will be several Megabytes. Save the Html in the same directory as the scraper and call it `2013.html` for the 2013 budget. The [`Read(int year)`](https://github.com/friism/dk-budget-parser/blob/master/EB.Budget.Parser/DataLoad/DataLoader.cs#L49) method can now read data for that year.

You can either load the data into a database of your choice, or just analyze it in-memory. The [`Exporter`](https://github.com/friism/dk-budget-parser/blob/master/EB.Budget.Parser/Export/Exporter.cs) class has various methods for exporting data to csv.