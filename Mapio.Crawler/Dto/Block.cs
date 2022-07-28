using System.Collections.Generic;
using System.Diagnostics;

namespace Mapio.Crawler.Dto
{
    [DebuggerDisplay("{DebugDisplay}")]
    public class Block
    {
        public string DbId { get; set; }

        public string Text { get; set; }

        public List<Level> Levels { get; set; } = new List<Level>();

        public string DebugDisplay => this.ToString();

        public override string ToString() => $"DbID {this.DbId}";
    }
}
