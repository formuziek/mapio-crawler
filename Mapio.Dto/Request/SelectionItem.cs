namespace Mapio.Dto.Request
{
    using System.Collections.Generic;

    public class SelectionItem
    {
        public string Filter { get; set; } = "item";

        public List<string> Values { get; set; } = new List<string>();
    }
}