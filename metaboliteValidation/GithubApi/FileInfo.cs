using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GithubApi
{
    public class FileInfo
    {
        public string path { get; set; }
        public string type { get; set; }
        public string mode { get; set; }
        public int size { get; set; }
        public string sha { get; set; }
        public string url { get; set; }
    }
}
