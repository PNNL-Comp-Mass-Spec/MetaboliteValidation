using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation
{
    class KeggUtil
    {
        public string Entry { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }
        public string ExactMass { get; set; }
        public string MolWeight { get; set; }
        public string Structure { get; set; }
        public string Remark { get; set; }
        public string Comment { get; set; }
        public string Reaction { get; set; }
        public string Pathway { get; set; }
        public string Module { get; set; }
        public string Enzyme { get; set; }
        public string Brite { get; set; }
        public string Reference { get; set; }
        public string OtherDb { get; set; }
        public string LinkDb { get; set; }
        public string KCFData { get; set; }
    }
}
