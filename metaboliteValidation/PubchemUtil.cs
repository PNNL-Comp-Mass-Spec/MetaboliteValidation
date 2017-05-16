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
        public Dictionary<int, Property> PubChemMap = new Dictionary<int, Property>();
        public PubchemUtil(string[] cids)
        {
            // make request to pubchem website
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl+ "/compound/cid/"+ string.Join(",",cids)+ "/Property/MolecularFormula,ExactMass,InChIKey/JSON");
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
            var pubResponse = (PubChemResponse)JsonConvert.DeserializeObject(reader.ReadToEnd(), typeof(PubChemResponse));
            // convert to a dictionary with cid as key to make comparisons
            ConverToMap(pubResponse.PropertyTable);
        }
        // converts array to dictionary with cid as key and values as the properties from pubchem
        private void ConverToMap(PropertyTable p)
        {
            foreach(var a in p.Properties)
            {
                if(!PubChemMap.ContainsKey(a.CID))
                    PubChemMap.Add(a.CID, a);
            }
        }
    }
    // classes to convert json string
    public class PubChemResponse
    {
        public PropertyTable PropertyTable { get; set; }
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
