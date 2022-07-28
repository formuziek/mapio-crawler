using System;
using System.Collections.Generic;
using System.Text;

namespace Mapio.Crawler.Dto
{
    public class Level
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }

        public List<Level> Levels { get; set; } = new List<Level>();

        public Table Table { get; set; }
    }
}
