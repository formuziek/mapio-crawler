namespace Mapio.Dto.Response
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Root
    {
        public List<Column> Columns { get; set; }

        public List<Data> Data { get; set; }
    }
}
