using System.Collections.Generic;

namespace MetaboliteValidation.GoodTableResponse
{
    public class Report
    {
        public Meta meta { get; set; }
        public Dictionary<string, ResultContext>[] results { get; set; }
    }
}
