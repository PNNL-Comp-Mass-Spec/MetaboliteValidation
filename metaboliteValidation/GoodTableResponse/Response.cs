using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GoodTableResponse
{
    public class Response
    {
        public Report report { get; set; }
        public Sources sources { get; set; }
        public bool success { get; set; }
    }
}
