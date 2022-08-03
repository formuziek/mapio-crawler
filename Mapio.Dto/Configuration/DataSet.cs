using System;
using System.Collections.Generic;
using System.Text;

namespace Mapio.Dto.Configuration
{
    public class DataSet
    {
        public string Version { get; set; }

        public string Uri { get; set; }

        public string Text { get; set; }

        public List<Variable> Variables { get; set; }
    }
}
