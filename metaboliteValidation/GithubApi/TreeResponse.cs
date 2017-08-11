using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GithubApi
{
    /**
         * json response from github to get a list of files
         */
    class TreeResponse
    {
        public string sha { get; set; }
        public string url { get; set; }
        public FileInfo[] tree { get; set; }
        public bool truncated { get; set; }
    }
}
