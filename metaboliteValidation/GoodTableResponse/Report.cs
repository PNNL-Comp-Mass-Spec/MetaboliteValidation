using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GoodTableResponse
{
    public class Report
    {
        public Meta meta { get; set; }
        public Dictionary<string, ResultContext>[] results { get; set; }
    }
}
