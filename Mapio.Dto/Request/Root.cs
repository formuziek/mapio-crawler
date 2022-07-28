namespace Mapio.Dto.Request
{
    using System.Collections.Generic;

    public class Root
    {
        public List<QueryItem> Query { get; set; } = new List<QueryItem>();

        public ResponseConfiguration Response { get; set; } = new ResponseConfiguration();
    }
}
