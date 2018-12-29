namespace CuisineScrapper
{
    using AngleSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var pageArray = new string[]
            {
                "http://www.cuisinedenotreterroirfrancais.com/termes2.php",
                "http://www.cuisinedenotreterroirfrancais.com/termes-culinaires-page-2.php",
                "http://www.cuisinedenotreterroirfrancais.com/thermes-culinaires-page-3.php",
            };

            var pages = new Dictionary<string, string>
            {
                [pageArray[0]] = "table:nth-child(27) , table:nth-child(22), table:nth-child(19), table:nth-child(15)",
                [pageArray[1]] = "table:nth-child(20) , table:nth-child(24), table:nth-child(16), table:nth-child(8), table:nth-child(12)",
                [pageArray[2]] = "table:nth-child(9) , table:nth-child(13), table:nth-child(17), table:nth-child(21), table:nth-child(25), :nth-child(29), :nth-child(33), :nth-child(36), :nth-child(40), table:nth-child(44)"
            };

            var config = Configuration.Default.WithDefaultLoader();

            var termesList = new List<Tuple<string, string>>();

            Parallel.ForEach(pageArray, async page =>
            {
                AngleSharp.Dom.IDocument document = await BrowsingContext.New(config).OpenAsync(page).ConfigureAwait(false);

                foreach (var table in document.DocumentElement.QuerySelectorAll(pages[page]))
                {
                    var tablesOfTheFirstTable = table.QuerySelectorAll("td")
                        .Select((x, i) => new { Index = i, Value = x })
                        .GroupBy(x => x.Index % 2 == 0)
                        .Select(x => x.Where(v => !string.IsNullOrEmpty(v.Value.TextContent.Trim()))
                        .Select(v => v.Value))
                        .ToList();

                    foreach (var line in Enumerable.Range(0, tablesOfTheFirstTable[0].Count()))
                    {
                        termesList.Add(Tuple.Create(tablesOfTheFirstTable[0].Skip(line).First().TextContent.Trim(),
                                             tablesOfTheFirstTable[1].Skip(line).First().TextContent.Trim()));
                    }
                }
            });

            foreach (var el in termesList)
            {
                Console.WriteLine($"{el.Item1} - {el.Item2}");
            }

            Console.ReadKey();
        }
    }
}