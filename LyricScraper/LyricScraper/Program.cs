using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using EurovisionCommon;

namespace LyricScraper
{
    static class Program
    {
        static HttpClient HttpClient;
        static ConcurrentBag<EurovisionSong> Songs;
        static EurovisionSongRepository SongRepository;

        static void Main(string[] args)
       {
            HttpClient = new HttpClient();
            Songs = new ConcurrentBag<EurovisionSong>();
            SongRepository = new EurovisionSongRepository();

            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Starting scraping");
            int startYear = 1956; //1956;
            int endYear = 2018;//2018;

            var tasks = new List<Task>();

            for (int year = startYear; year <= endYear; year++)
            {
                //await DownloadScrapeSongsForYear(year);
                tasks.Add(DownloadScrapeSongsForYear(year));               
            }

            await Task.WhenAll(tasks);
        }

        private static async Task DownloadScrapeSongsForYear(int year)
        {
            var browser = new ScrapingBrowser();

            var url = new Uri($"https://4lyrics.eu/eurovision/esc-{year}/");

            var songListPage = browser.NavigateToPage(url);

            // 3rd results table
            var songsTable = songListPage.Html.CssSelect("table#results-table").ElementAt(3);

            
            var songData = songsTable
                            .CssSelect("td")
                            .Where(s => s.Id != "results-td-header")
                            .Where(s => s.Id != "results-td-blank")
                            .ToList(); //  header cells

  
            for (int index = 0; index < songData.Count; index += 4)
            {

                var song = new EurovisionSong
                {
                    Artist = HttpUtility.HtmlDecode(songData[index + 2].InnerText),
                    Country = HttpUtility.HtmlDecode(songData[index + 1].InnerText),
                    Title = HttpUtility.HtmlDecode(songData[index + 3].InnerText),
                    Winner = songData[index + 2].HasClass("results-td-winner"),
                    Year = year
                };

                song.Process();

 
                var lyricsUrl = songData[index + 3].FirstChild?.Attributes["href"]?.Value;
                if (!string.IsNullOrEmpty(lyricsUrl))
                {
                    ScrapeLyrics(song, lyricsUrl, url);

                    Console.WriteLine($"Saving {song.ToString()}");
                    var result = await SongRepository.UpsertItemAsync(song);
                    Songs.Add(song);
                }
            }
        }

        private static void ScrapeLyrics(EurovisionSong song, string partialUrl, Uri songlistUrl)
        {
            Console.WriteLine($"Downloading lyrics for: {song.Year} {song.Country} {song.Title}");

            Uri url;
            if (partialUrl.StartsWith("/"))
            {
                url = new Uri($"https://4lyrics.eu/{partialUrl}");
            }
            else
            {
                url = songlistUrl.Combine(partialUrl);
            }
            
            var browser = new ScrapingBrowser();

            var lyricsPage = browser.NavigateToPage(url);

            // Check if foreign language page
            var language = lyricsPage.Html.CssSelect("a.GTTabsLinks").FirstOrDefault()?.InnerText;
            var lyrics = lyricsPage.Html.CssSelect("div.GTTabs_divs.GTTabs_curr_div").FirstOrDefault();

            if (string.IsNullOrEmpty(language))
            {
                ScrapeEnglishOnlyLyrics(song, lyricsPage);
            }
            else
            {
                song.Language = language;

                // First paragraph is hidden
                var hiddenNode = lyrics.ChildNodes
                                        .First(c => c.Name == "span");
                lyrics.RemoveChild(hiddenNode);

                song.Lyrics = HttpUtility.HtmlDecode(lyrics.InnerText);                
            }
            
        }



        private static void ScrapeEnglishOnlyLyrics(EurovisionSong song, WebPage lyricsPage)
        {
            var lyricContainer = lyricsPage.Html.CssSelect("div")
                                    .First(d => d.Attributes.FirstOrDefault()?.Name == "itemprop" 
                                                && d.Attributes.FirstOrDefault()?.Value == "text");

            // Remove Link to eurostar page
            var link = lyricContainer.ChildNodes.FirstOrDefault(c => c.Name == "h3");
            if (link != null)
            {
                lyricContainer.RemoveChild(link);
            }

            var lyrics = lyricContainer.CssSelect("p").Where(p => p.FirstChild?.Name == "#text");

            song.Language = "English";
            song.Lyrics = string.Join("\n\n", lyrics.Select(l => HttpUtility.HtmlDecode(l.InnerText)));
        }
    }
}
