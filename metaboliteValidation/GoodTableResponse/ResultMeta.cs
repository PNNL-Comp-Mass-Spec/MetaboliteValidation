using Newtonsoft.Json;
using System.Collections.Generic;

namespace MetaboliteValidation.GoodTableResponse
{
    public class ResultMeta
    {
        [JsonProperty("")]
        public Dictionary<string, ResultContext> obj { get; set; }
    }
}
