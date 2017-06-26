using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GoodTableResponse
{
    public class Meta
    {
        public int bad_column_count { get; set; }
        public int bad_row_count { get; set; }
        public Column[] columns { get; set; }
        public string encoding { get; set; }
        public int header_index { get; set; }
        public string name { get; set; }
        public int row_count { get; set; }
        public string[] headers { get; set; }
    }
}
