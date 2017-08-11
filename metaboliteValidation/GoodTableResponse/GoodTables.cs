using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GoodTableResponse
{
    public class GoodTables
    {
        public string source { get; set; }
        public string schema { get; set; }
        public Response Response { get; set; }
        public GoodTables(string source, string schema)
        {
            this.source = source;
            this.schema = schema;
            init();
        }
        private void init()
        {
            var goodtableData = new GoodtableData(source,schema);
            

            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, "http://goodtables.okfnlabs.org/api/run"))
                {
                    req.Content = new StringContent(JsonConvert.SerializeObject(goodtableData), Encoding.UTF8);
                    req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    using (HttpResponseMessage resp = client.SendAsync(req).Result)
                    {
                        try
                        {
                            resp.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine("Unauthorized, username, password is incorrect or you don't have access to this repository.");
                            }
                        }
                        Response = JsonConvert.DeserializeObject<Response>(resp.Content.ReadAsStringAsync().Result);
                    }

                }
            }
        }

        internal void OutputResponse(StreamWriter streamWriter)
        {
            streamWriter.Write(JsonConvert.SerializeObject(Response, Formatting.Indented));
            streamWriter.Close();
        }

        internal void OutputResponse(TextWriter @out)
        {
            @out.Write(JsonConvert.SerializeObject(Response, Formatting.Indented));
        }
    }
}
