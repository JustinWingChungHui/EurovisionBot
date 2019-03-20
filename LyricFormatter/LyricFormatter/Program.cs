using EurovisionCommon;
using LyricRobotCommon;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LyricFormatter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started");
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {

            Console.WriteLine("Downloading eurovision songs");
            var eurovisionRepo = new EurovisionSongRepository();

            // Only get songs in English after 1998
            var eurovisionSongs = await eurovisionRepo.GetItemsAsync(x => x.Language == "English" && x.Year > 1998, -1);

            var songs = new List<SongRecord>();

            DocumentDBRepository<SongRecord>.Initialize();

            foreach (var euroSong in eurovisionSongs)
            {
                var song = new SongRecord
                {
                    id = euroSong.id,
                    Artist = euroSong.Artist,
                    Lyrics = euroSong.Lyrics,
                    Genre = new List<string> { Genre.Eurovision.ToString(), Genre.Pop.ToString() },
                    LyricsDownloaded = true,
                    Released = new DateTime(euroSong.Year, 5, 1),
                    Title = euroSong.Title
                };

                Console.WriteLine($"Saving {song.id}");

                songs.Add(song);

                await DocumentDBRepository<SongRecord>.UpsertItemAsync(song.id, song);
            }
        }
    }
}
