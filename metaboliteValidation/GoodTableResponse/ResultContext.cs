using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GoodTableResponse
{
    public class ResultContext
    {
        public string[] result_context { get; set; }
        public Result[] results { get; set; }
        public int row_index { get; set; }
    }
}
