using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation
{
    [Obsolete("No longer used")]
    public class ValidatePubchem
    {
        private readonly Dictionary<int, Property> _pubchem;
        private readonly DelimitedFileParser _parser;
        private readonly List<string> _errorReport = new List<string>();
        
        public ValidatePubchem(Dictionary<int, Property> pubchem, DelimitedFileParser parser)
        {
            this._pubchem = pubchem;
            this._parser = parser;
            Validate();
        }
        private void Validate()
        {
            var headers = _parser.GetHeaderMap();
            var count = 0;
            foreach (var a in _parser.GetRows())
            {
                if (a.Length <= 1)
                    continue;
                var cid = -1;
                if (a[headers["cid"]].Length > 0)
                    cid = int.Parse(a[headers["cid"]]);
                var formula = a[headers["formula"]];
                string inchi = null;
                if (headers.ContainsKey("InChi Key"))
                    inchi = a[headers["InChi Key"]];
                float exactMass = 0;
                if (a[headers["mass"]].Length>0)
                    exactMass = float.Parse(a[headers["mass"]]);
                if (cid != -1)
                {
                    var actual = new Property
                    {
                        CID = cid,
                        ExactMass = exactMass,
                        InChIKey = inchi,
                        MolecularFormula = formula
                    };
                    var expected = _pubchem[cid];
                    if (!expected.Equals(actual))
                        AddToError(expected, actual, count);
                }
                else
                {
                    AddToError("no cid", count);
                }
                count++;
            }
        }

        private void AddToError(string str, int row)
        {
            var builder = new StringBuilder();
            builder.Append("Error with ")
                .Append("row ")
                .Append(row)
                .Append(".\n")
                .Append(str)
                .Append("\n");
            _errorReport.Add(builder.ToString());
        }
        private void AddToError(Property expecting, Property actual, int row)
        {
            var builder = new StringBuilder();
            builder.Append("Error with ")
                .Append(expecting.CID)
                .Append(", row ")
                .Append(row)
                .Append(".\n")
                .Append($"{"",10}{"Mass",20}{"Formula",20}{"InchIKey",60}\n")
                .Append($"{"Expected",10}{expecting.ExactMass,20}{expecting.MolecularFormula,20}{expecting.InChIKey,60}\n")
                .Append($"{"Actual",10}{actual.ExactMass,20}{actual.MolecularFormula,20}{actual.InChIKey,60}\n");
            _errorReport.Add(builder.ToString());
        }
        public string PrintError()
        {
            return string.Join("\n", _errorReport.ToArray());
        }
    }
}
