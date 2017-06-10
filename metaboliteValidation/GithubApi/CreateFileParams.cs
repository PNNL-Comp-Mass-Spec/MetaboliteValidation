using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GithubApi
{
    /**
    * json parameters for creating a file in a repository on github
    */
    class CreateFileParams
    {
        public string path { get; set; }
        public string message { get; set; }
        public string content { get; set; }
        public string branch { get; set; }
    }
}
