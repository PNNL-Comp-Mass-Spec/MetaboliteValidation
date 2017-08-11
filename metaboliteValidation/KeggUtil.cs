using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation
{
    public class KeggUtil
    {
        public List<CompoundData> Compounds = new List<CompoundData>();
        public Dictionary<string, CompoundData> CompoundsMap { get;set;}
        public KeggUtil(string[] ids)
        {
            CompoundsMap = new Dictionary<string, CompoundData>();
            var maxEntries = 10;
            var index = (int)ids.Count() / maxEntries;
            for (int i = 0; i <= index; i++)
            {
                Console.WriteLine("Retrieving mass and formula info from Kegg: {0} / {1}", i + 1, index + 1);

                var splitIds = ids.ToList().GetRange(i * maxEntries, Math.Min(maxEntries, ids.Count() - i * maxEntries));
                using (var client = new HttpClient())
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Get, "http://rest.kegg.jp/get/" + String.Join("+", splitIds)))
                    {
                        using (HttpResponseMessage resp = client.SendAsync(req).Result)
                        {
                            try
                            {
                                resp.EnsureSuccessStatusCode();
                            }
                            catch (Exception e)
                            {

                            }
                            string[] comps = resp.Content.ReadAsStringAsync().Result.Split(new string[] { "///\n" }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            CompoundsMap = new Dictionary<string, CompoundData>();
                            foreach (var str in comps)
                            {
                                Compounds.Add(ReadKeggCompoundStream(str));
                            }
                        }
                    }
                }
                toCompoundMap(Compounds);
            }
        }
        private void toCompoundMap(List<CompoundData> list)
        {
            foreach (var a in list)
            {
                if (!CompoundsMap.ContainsKey(a.KeggId))
                    CompoundsMap.Add(a.KeggId, a);
            }
        }
        public CompoundData ReadKeggCompoundStream(string page)
        {

            var lines = page.Split('\n');

            CompoundData entryData = null;
            for(var i = 0;i< lines.Length;i++)
            {
                var line = lines[i];
                string[] tokens;
                if (line.ToLower().StartsWith("entry"))
                {
                    //System.Console.WriteLine(line);
                    tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    entryData = new CompoundData(tokens[1]);
                    entryData.Type = tokens[2];
                }
                if (line.ToLower().StartsWith("name"))
                {
                    tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    entryData.Names.Add(tokens[1]);
                    line = lines[++i];
                    while (line != null && char.IsWhiteSpace(line[0]))
                    {
                        entryData.Names.Add(line.Trim());
                        line = lines[++i];
                    }
                }
                if (line != null && line.ToLower().StartsWith("formula"))
                {
                    tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    entryData.Formula = tokens[1];
                    line = lines[++i];
                }
                if (line != null && line.ToLower().StartsWith("exact_mass"))
                {
                    tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    entryData.ExactMass = double.Parse(tokens[1]);
                    line = lines[++i];
                }
                if (line != null && line.ToLower().StartsWith("mol_weight"))
                {
                    tokens = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    entryData.MolecularWeight = double.Parse(tokens[1]);
                    line = lines[++i];
                }
                if (line != null && line.ToLower().StartsWith("comment"))
                {
                    line = line.Remove(0, 7);
                    entryData.Comment = line.Trim();
                    line = lines[++i];
                }
                if (line != null && line.ToLower().StartsWith("pathway"))
                {
                    line = line.Remove(0, 7);
                    while (line != null && char.IsWhiteSpace(line[0]))
                    {
                        tokens = line.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        entryData.Pathways.Add(tokens[0]);
                        line = lines[++i];
                    }
                }
                if (line != null && line.ToLower().StartsWith("dblinks"))
                {
                    line = line.Remove(0, 7);
                    while (line != null && char.IsWhiteSpace(line[0]))
                    {
                        tokens = line.Trim().Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
                        var identifiers = tokens[1].Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var identifier in identifiers)
                        {
                            entryData.OtherIds.Add(new KeyValuePair<string, string>(tokens[0], identifier));
                        }
                        line = lines[++i];
                    }
                }
            }
            return entryData;
        }
    }
    public class CompoundData
    {
        public string KeggId { get; private set; }
        public string Type { get; set; }
        public List<string> Names { get; private set; }
        public string Formula { get; set; }
        public double ExactMass { get; set; }
        public double MolecularWeight { get; set; }
        public string Comment { get; set; }
        public List<string> Pathways { get; private set; }
        public List<KeyValuePair<string, string>> OtherIds { get; private set; }

        public CompoundData(string keggId)
        {
            KeggId = keggId;
            Names = new List<string>();
            Pathways = new List<string>();
            OtherIds = new List<KeyValuePair<string, string>>();
            Type = string.Empty;
            Formula = string.Empty;
            Comment = string.Empty;
        }
        public string OtherId(string query)
        {
            var keyValuePair = OtherIds.Where(x => x.Key.Equals(query)).ToList().FirstOrDefault();
            if (keyValuePair.Equals(default(KeyValuePair<string,string>)))
                return "";
            return keyValuePair.Value;
        }
        public override string ToString()
        {
            return this.KeggId;
        }
    }
}
