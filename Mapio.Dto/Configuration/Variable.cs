using System;
using System.Collections.Generic;
using System.Text;

namespace Mapio.Dto.Configuration
{
    public class Variable
    {
        public string Code { get; set; }

        public string Text { get; set; }

        public List<ValueItem> ValueItems { get; set; }
    }
}
