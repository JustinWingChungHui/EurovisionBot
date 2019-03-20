using System;
using System.Collections.Generic;
using System.Linq;

namespace EurovisionCommon
{
    public class EurovisionSong
    {
        public string id { get; set; }
        public int Year { get; set; }
        public string Country { get; set; }
        public string Artist { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Lyrics { get; set; }
        public bool Winner { get; set; }

        public override string ToString()
        {
            return $"{Year} {Country} {Artist} {Title}";
        }

        public void Process()
        {
            var titleNoPunctuation = new string(Title.Where(c => !char.IsPunctuation(c)).ToArray());
            Slug = titleNoPunctuation.Replace(' ', '-');
            id = $"{Year}_{Country}_{Slug}";
        }
    }
}
