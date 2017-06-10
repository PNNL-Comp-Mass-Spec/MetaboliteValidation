using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GithubApi
{
    /**
    * json parameters for updating a file in a repository on github
    */
    class UploadFileParams : CreateFileParams
    {
        public string sha { get; set; }
    }
}
