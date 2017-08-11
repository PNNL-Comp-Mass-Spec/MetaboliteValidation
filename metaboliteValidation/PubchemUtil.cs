using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace metaboliteValidation
{
    public class PubchemUtil
    {
        private const string BaseUrl = "https://pubchem.ncbi.nlm.nih.gov/rest/pug";
        public Dictionary<int, Compound> PubChemMap = new Dictionary<int, Compound>();
        public PubchemUtil(string[] cids)
        {
            var maxEntries = 100;
            var index = (int)cids.Count() / maxEntries;
            for (int i = 0; i <= index; i++)
            {
                Console.WriteLine("Retrieving mass and formula info from PubChem: {0} / {1}", i + 1, index + 1);

                var splitCids = cids.ToList().GetRange(i* maxEntries, Math.Min(maxEntries, cids.Count() - i* maxEntries));

                // make request to pubchem website
                var request = (HttpWebRequest)WebRequest.Create(BaseUrl + "/compound/cid/" + string.Join(",", splitCids) + "/JSON");
                // get response
                var response = (HttpWebResponse)request.GetResponse();
                // read stream
                var resStream = response.GetResponseStream();
                if (resStream == null)
                {
                    throw new NullReferenceException();
                }
                var reader = new StreamReader(resStream);
                // Convert string to json
                var pubResponse = (PcCompounds)JsonConvert.DeserializeObject(reader.ReadToEnd(), typeof(PcCompounds));
                // convert to a dictionary with cid as key to make comparisons
                ConverToMap(pubResponse.PC_Compounds);
            }

        }
        // converts array to dictionary with cid as key and values as the properties from pubchem
        private void ConverToMap(List<Compound> p)
        {
            foreach (var a in p)
            {
                if (!PubChemMap.ContainsKey(a.getId()))
                    PubChemMap.Add(a.getId(), a);
            }
        }
    }
    // classes to convert json string
    public class PubChemResponse
    {
        public PcCompounds PropertyTable { get; set; }
    }
    public class PcCompounds
    {
        public List<Compound> PC_Compounds { get; set; }
    }
    public class Compound
    {
        public Id id { get; set; }
        public Atoms atoms { get; set; }
        public Bonds bonds { get; set; }
        public List<Stereo> stereo { get; set; }
        public List<Coord> coords { get; set; }
        public int charge { get; set; }
        public List<Prop> props { get; set; }
        public Dictionary<string, int> count { get; set; }
        public Value findProp(string query)
        {
            return props.Where(x => x.urn.label !=null && x.urn.label.Equals(query)|| x.urn.name != null && x.urn.name.Equals(query)).ToList().First().value;
        }
        public int getId()
        {
            return id.id["cid"];
        }
    }
    public class Prop
    {
        public Urn urn { get;set; }
        public Value value { get; set; }
    }
    public class Urn
    {
        public string label { get; set; }
        public string name { get; set; }
        public int datatype { get; set; }
        public string implementation { get; set; }
        public string version { get; set; }
        public string software { get; set; }
        public string source { get; set; }
        public string release { get; set; }
    }
    public class Value
    {
        public string sval { get; set; }
        public double fval { get; set; }
        public int ival { get; set; }
        public string binary { get; set; }
    }

    public class Id
    {
        public Dictionary<string, int> id { get; set; }
    }
    public class Atoms
    {
        public List<int> aid { get; set; }
        public List<int> element { get; set; }
    }
    public class Bonds
    {
        public List<int> aid1 { get; set; }
        public List<int> aid2 { get; set; }
        public List<int> order { get; set; }
    }
    public class Stereo
    {
        public Dictionary<string, int> tetrahedral { get; set; }
    }
    public class Coord
    {
        public List<int> type { get; set; }
        public List<int> aid { get; set; }
        public List<Conformer> conformers { get; set; }
    }
    public class Conformer
    {
        public List<double> x { get; set; }
        public List<double> y { get; set; }
        public Style style { get; set; }
    }
    public class Style
    {
        public List<int> annotation { get; set; }
        public List<int> aid1 { get; set; }
        public List<int> aid2 { get; set; }
    }
    public class PropertyTable
    {
        public List<Property> Properties { get; set; }
    }
    public class Property
    {
        public int CID { get; set; }
        public string MolecularFormula { get; set; }
        public string InChIKey { get; set; }
        public float ExactMass { get; set; }

        protected bool Equals(Property other)
        {
            return CID == other.CID && string.Equals(MolecularFormula, other.MolecularFormula) && string.Equals(InChIKey, other.InChIKey) && ExactMass.Equals(other.ExactMass);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Property) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CID;
                hashCode = (hashCode * 397) ^ (MolecularFormula != null ? MolecularFormula.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InChIKey != null ? InChIKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ExactMass.GetHashCode();
                return hashCode;
            }
        }
    }
}
