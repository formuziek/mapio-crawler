using System.Collections.Generic;
using System.Diagnostics;

namespace Mapio.Crawler.Dto
{
    public class Block
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }

        public List<Block> Children { get; set; } = new List<Block>();

        public Response Table { get; set; }
    }
}
