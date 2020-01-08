namespace CuisineScraper
{
    using AngleSharp;
    using AngleSharp.Dom;
    using AngleSharp.Dom.Html;
    using CsvHelper;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    internal static class Program
    {
        private static async Task Main()
        {
            var webPagesAndSelectors = new Dictionary<string, string>
            {
                ["http://www.cuisinedenotreterroirfrancais.com/termes2.php"] = "table:nth-child(27) , table:nth-child(22), table:nth-child(19), table:nth-child(15)",
                ["http://www.cuisinedenotreterroirfrancais.com/termes-culinaires-page-2.php"] = "table:nth-child(20) , table:nth-child(24), table:nth-child(16), table:nth-child(8), table:nth-child(12)",
                ["http://www.cuisinedenotreterroirfrancais.com/thermes-culinaires-page-3.php"] = "table:nth-child(9) , table:nth-child(13), table:nth-child(17), table:nth-child(21), table:nth-child(25), :nth-child(29), :nth-child(33), :nth-child(36), :nth-child(40), table:nth-child(44)"
            };

            var records = await GetRecordsAsync(webPagesAndSelectors).ConfigureAwait(false);

            using (TextWriter textWriter = new StreamWriter(@".\output.csv"))
            using (CsvWriter csv = new CsvWriter(textWriter))
            {
                csv.Configuration.HasHeaderRecord = false;
                csv.WriteRecords(records);
            }
        }

        private static async Task<ConcurrentBag<(string, string)>> GetRecordsAsync(Dictionary<string, string> webPagesAndSelectors)
        {
            var angleConfig = Configuration.Default.WithDefaultLoader();
            var records = new ConcurrentBag<(string, string)>();

            var tasks = webPagesAndSelectors.Select(async pageAndSelector =>
            {
                var document = await BrowsingContext.New(angleConfig).OpenAsync(pageAndSelector.Key).ConfigureAwait(false);

                foreach (var table in document.DocumentElement.QuerySelectorAll(pageAndSelector.Value))
                {
                    var dataframe = table.QuerySelectorAll("td")
                        .Select((x, i) => new { Index = i, Value = x })
                        .GroupBy(x => x.Index % 2 == 0)
                        .Select(x => x.Where(v => !string.IsNullOrWhiteSpace(v.Value.TextContent))
                        .Select(v => v.Value))
                        .ToList();

                    var totalRows = dataframe[0].Count();
                    for (var rowNumber = 0; rowNumber < totalRows; rowNumber++)
                    {
                        records.Add(GetRow(dataframe, rowNumber));
                    }
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return records;
        }

        private static (string, string) GetRow(List<IEnumerable<IElement>> column, int rowNumber)
        {
            var firstColumn = column[0].Skip(rowNumber).First();
            var secondColumn = column[1].Skip(rowNumber).First();

            return (ExtractColumn(firstColumn), ExtractColumn(secondColumn));
        }

        private static string ExtractColumn(IElement column)
        {
            var columnText = column.TextContent.Trim();
            var columnImage = column.QuerySelectorAll("a");

            if (!columnImage.Any())
            {
                return columnText;
            }

            var imagesRoot = new XElement("div");
            foreach (var el in columnImage?.OfType<IHtmlAnchorElement>())
            {
                var imageUrl = new Uri(el.Href);
                var fileName = Path.GetFileName(imageUrl.LocalPath);

                CreateFile(imageUrl, fileName);
                imagesRoot.Add(new XElement("img", new XAttribute("src", fileName)));
            }
            return $"{columnText}<br>{imagesRoot.ToString(SaveOptions.DisableFormatting)}".Replace('"', '\'');
        }

        private static void CreateFile(Uri imageUrl, string fileName)
        {
            if (!File.Exists(fileName))
            {
                using (var myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(imageUrl, fileName);
                }
            }
        }
    }
}