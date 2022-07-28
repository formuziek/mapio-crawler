namespace Mapio.Crawler.Dto
{
    using System.Collections.Generic;

    public class Variable
    {
        public string Code { get; set; }

        public string Text { get; set; }

        public List<string> Values { get; set; }

        public List<string> ValueTexts { get; set; }

        public bool? Elimitation { get; set; }
    }
}