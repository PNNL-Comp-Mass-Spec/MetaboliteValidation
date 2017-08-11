using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GoodTableResponse
{
    public class Result
    {
        public int colum_index { get; set; }
        public string column_name { get; set; }
        public string processor { get; set; }
        public string result_category { get; set; }
        public string result_id { get; set; }
        public string result_level { get; set; }
        public string result_message { get; set; }
        public string result_name { get; set; }
        public string row_name { get; set; }
    }
}
