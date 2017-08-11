using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace metaboliteValidation
{
    public class DelimitedFileParser
    {
        /// <summary>
        /// Header names; stored as lowercase
        /// </summary>
        private readonly List<string> _headers = new List<string>();

        /// <summary>
        /// Header names; stored in the original case
        /// </summary>
        private readonly List<string> _headersOriginal = new List<string>();

        [Obsolete("No longer used")]
        private string[][] _full;

        [Obsolete("No longer used")]
        private string[][] _reverse;

        /// <summary>
        /// Full list of loaded data, with one dictionary per row
        /// </summary>
        /// <remarks>In the dictionary, keys are column names and values are column data</remarks>
        public readonly List<Dictionary<string, string>> FullMap = new List<Dictionary<string, string>>();

        public readonly Dictionary<string, List<string>> ReverseMap = new Dictionary<string, List<string>>();
        private int _columnLength;
        private int _rowLength;
        private char _delimiter;
        private readonly Dictionary<string, int> _headerInverse = new Dictionary<string, int>();
        public DelimitedFileParser()
        {
            _delimiter = ',';
        }
        ///
        /// <summary>This function will parse a delimited string</summary>
        /// <param name="content">The delimited string</param>
        /// <param name="delimiter">The character used for dilimiting the string</param>
        /// <param name="header">Boolean if the first row is a header</param>
        public void ParseString(string content, char delimiter = ',', bool header = true)
        {
            this._delimiter = delimiter;
            Serialize(content, delimiter, header);
        }
        ///
        /// <summary>This function will parse a delimited file by reading the file to a string</summary>
        /// <param name="content">The path to the delimited file</param>
        /// <param name="delimiter">The character used for dilimiting the string</param>
        /// <param name="header">Boolean if the first row is a header</param>
        public void ParseFile(string fileName, char delimiter = ',', bool header = true)
        {
            // find file
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }
            this._delimiter = delimiter;
            string content = File.ReadAllText(fileName);
            Serialize(content, delimiter, header);
        }
        /// <summary>
        /// Private function will parse a delimited string
        /// </summary>
        /// <param name="content">
        /// The delimited string
        /// </param>
        /// <param name="delimiter">
        /// The character used for dilimiting the string
        /// </param>
        /// <param name="header">
        /// Boolean if the first row is a header
        /// </param>
        [Obsolete("No longer used")]
        private void Parse(string content, char delimiter, bool header)
        {
            // remove return carrage symbol
            content = content.Replace("\r", String.Empty);
            // parse file
            var lines = content.Split('\n');
            _rowLength = lines.Length;
            // track column types?

            // if header. track headers in delimited file
            if (header)
            {
                SetHeaders(lines[0].Split(_delimiter));
                var temp = new List<string>(lines);
                temp.RemoveAt(0);
                lines = temp.ToArray();
                _rowLength--;
            }
            if (lines[lines.Length - 1].Length == 0)
            {
                var temp = new List<string>(lines);
                temp.RemoveAt(temp.Count - 1);
                lines = temp.ToArray();
                _rowLength--;
            }
            this._columnLength = lines[0].Split(this._delimiter).Length;


            var iLength = lines.Length;
            var jLength = lines[0].Split(this._delimiter).Length;

            _full = new string[iLength][];
            _reverse = new string[jLength][];
            for (var i = 0; i < lines.Length; i++)
            {
                if(lines[i].Length<=0) continue;
                var str = lines[i].Split(this._delimiter);
                _full[i] = new string[str.Length];

                for (var j = 0; j < str.Length; j++)
                {
                    if (_reverse[j] == null || _reverse[j].Length == 0)
                        _reverse[j] = new string[lines.Length];
                    _full[i][j] = str[j];
                    _reverse[j][i] = str[j];
                }
            }
        }
        public void Serialize(string content, char delimiter, bool header)
        {
            var lines = content.Split('\n');
            _rowLength = lines.Length;
            // if header. track headers in delimited file
            if (header)
            {
                SetHeaders(lines[0].Split(_delimiter));
                var temp = new List<string>(lines);
                temp.RemoveAt(0);
                lines = temp.ToArray();
                _rowLength--;
            }
            // remove empty last row
            if (lines[lines.Length - 1].Length == 0)
            {
                var temp = new List<string>(lines);
                temp.RemoveAt(temp.Count - 1);
                lines = temp.ToArray();
                _rowLength--;
            }

            var rowNumber = 1;

            for (var i = 0; i < lines.Length; i++)
            {
                rowNumber++;

                // ignore empty rows
                if (lines[i].Length <= 0)
                    continue;

                Dictionary<string, string> tempMap = new Dictionary<string, string>();
                var rowData = lines[i].Split(this._delimiter).ToList();

                if (rowData.Count < _headers.Count)
                {
                    Console.WriteLine("Warning, row {0} has fewer columns than expected: {1} vs. {2}",
                        rowNumber, rowData.Count, _headers.Count);

                    // Append the remaining columns
                    while (rowData.Count < _headers.Count)
                    {
                        rowData.Add(string.Empty);
                    }

                }

                for (var j = 0; j < _headers.Count; j++)
                {
                    tempMap.Add(_headers[j], rowData[j].Trim().Trim('\uFFFD'));
                    if (!ReverseMap.ContainsKey(_headers[j]))
                        ReverseMap.Add(_headers[j], new List<string>());
                    ReverseMap[_headers[j]].Add(rowData[j].Trim().Trim('\uFFFD'));
                }

                FullMap.Add(tempMap);
            }
        }

        internal void SetDelimiter(char v)
        {
            _delimiter = v;
        }

        internal void SetHeaders(IEnumerable<string> newHeaders)
        {
            if (ReverseMap.Count > 0)
            {
                throw new Exception("Cannot set headers after ReverseMap has been populated; consider using UpdateHeaders instead");
            }

            _headers.Clear();
            foreach (var header in newHeaders)
            {
                var headerToStore = header.ToLower().Trim();
                ReverseMap.Add(headerToStore, new List<string>());
                _headers.Add(headerToStore);
                _headersOriginal.Add(header);
            }

            UpdateInverseHeaders();
        }

        /// <summary>
        /// Update specific header names
        /// </summary>
        /// <param name="headerMapping">Dictionary mapping old name to new name (only include header names that need to change)</param>
        internal void UpdateHeaders(IReadOnlyDictionary<string, string> headerMapping)
        {
            foreach (var mapping in headerMapping)
            {
                for (var i = 0; i < _headers.Count; i++)
                {
                    var oldHeader = mapping.Key.ToLower();
                    var newHeader = mapping.Value.ToLower();

                    if (_headers[i] != mapping.Key)
                        continue;

                    _headers[i] = newHeader;

                    if (ReverseMap.TryGetValue(oldHeader, out var items))
                    {
                        ReverseMap.Remove(oldHeader);
                        ReverseMap.Add(newHeader, items);
                    }
                    else
                    {
                        ReverseMap.Add(newHeader, new List<string>());
                    }

                    break;
                }
            }

        }

        private void UpdateInverseHeaders()
        {
            _headerInverse.Clear();
            for (var i = 0; i < _headers.Count; i++)
            {
                _headerInverse.Add(_headers[i], i);
            }
        }

        internal int Count()
        {
            return FullMap.Count;
        }

        internal void Remove(Dictionary<string, string> a)
        {
            FullMap.Remove(a);
        }

        internal void Add(Dictionary<string, string> a)
        {
            FullMap.Add(a);
            foreach (var key in a.Keys)
            {
                if (!ReverseMap.ContainsKey(key))
                    ReverseMap.Add(key, new List<string>());
                ReverseMap[key].Add(a[key]);
            }
        }

        /// <summary>
        /// Retrieve the complete set of loaded data
        /// </summary>
        /// <returns>Dictionary where </returns>
        public List<Dictionary<string, string>> GetMap()
        {
            return FullMap;
        }

        public Dictionary<string, string> GetAt(int index)
        {
            return FullMap[index];
        }
        public string GetPropertyAt(int index, int key)
        {
            return FullMap[index][_headers[key]];
        }
        public string GetPropertyAt(int index, string key)
        {
            return FullMap[index][key];
        }
        public List<string> GetColumnAt(int index)
        {
            return ReverseMap[_headers[index]];
        }
        public List<string> GetColumnAt(string key)
        {
            try
            {
                return ReverseMap[key];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Key not found {0}",  key);
                throw;
            }

        }
        public Dictionary<string, int> GetHeaderMap()
        {
            return this._headerInverse;
        }

        [Obsolete("No longer used")]
        public string[] GetRow(int index)
        {
            if (index < 0 || index >= _full.Length)
                throw new IndexOutOfRangeException();
            return _full[index];
        }

        [Obsolete("No longer used")]
        public string[] GetCol(int index)
        {
            if (index < 0 || index >= _reverse.Length)
                throw new IndexOutOfRangeException();
            return _reverse[index];
        }

        [Obsolete("No longer used")]
        public string[] GetCol(string colName)
        {
            if(_headerInverse.ContainsKey(colName))
                return _reverse[_headerInverse[colName]];
            return null;
        }

        public List<string> GetHeaders()
        {
            return _headers;
        }

        public List<string> GetHeadersOriginalCase()
        {
            return _headersOriginal;
        }

        [Obsolete("Use GetMap instead")]
        public string GetAt(int row, int col)
        {
            return _full[row][col];
        }

        public int ColumnSize()
        {
            return this._columnLength;
        }

        public int RowSize()
        {
            return this._rowLength;
        }

        [Obsolete("Use GetMap instead")]
        public string[][] GetRows()
        {
            return _full;
        }

        /// <summary>This function will compare two string arrays presuming they are the headers</summary>
        /// <param name="a">The left side of the compare</param>
        /// <param name="b">The right side of the compare</param>
        /// <returns>If the two string arrays are the same returns true, false otherwise.</returns>
        private bool CompareHeaders(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;

            for(var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>This function will append a DelimitedFileParser to the end of this class</summary>
        /// <param name="a">The DelimietedFileParser to add to this class</param>
        //public bool Concat(DelimitedFileParser a)
        //{
        //    if (CompareHeaders(a.GetHeaders(), _headers))
        //    {
        //        var replacement = new string[_full.Length+a.GetRows().Length][];
        //        var i = 0;
        //        for (i = 0; i < _full.Length;i++)
        //        {
        //            replacement[i] = _full[i];
        //        }
        //        for (var j = 0; j < a.GetRows().Length; j++)
        //        {
        //            replacement[i + j] = a.GetRows()[j];
        //        }
        //        _full = replacement;
        //        return true;
        //    }
        //    return false;
        //}
        public bool Concat(DelimitedFileParser a)
        {
            if (CompareHeaders(a.GetHeaders(), _headers))
            {
                FullMap.AddRange(a.GetMap());
                foreach (var h in _headers)
                {
                    ReverseMap[h].AddRange(a.GetColumnAt(h));
                }
                return true;
            }

            Console.WriteLine();
            Console.WriteLine("Concatenation of new records failed; header name mismatch");
            Console.WriteLine("New data:      " + string.Join(", ", a.GetHeaders()));
            Console.WriteLine("Existing data: " + string.Join(", ", _headers));

            return false;
        }

        /// <summary>Converts to a delimited string using the provided delimiter</summary>
        /// <returns>A delimited string</returns>
        //public override string ToString()
        //{
        //    var result = "";
        //    var firstHead = true;
        //    foreach (var head in _headers)
        //    {
        //        if (!firstHead)
        //            result += _delimiter;
        //        firstHead = false;
        //        result += head;
        //    }
        //    foreach (var row in _full)
        //    {
        //        if (!firstHead)
        //            result += "\n";
        //        firstHead = false;
        //        var firstCol = true;
        //        foreach (var col in row)
        //        {
        //            if (!firstCol)
        //                result += _delimiter;
        //            firstCol = false;
        //            result += col;
        //        }
        //    }
        //    return result;
        //}

        public override string ToString()
        {
            return string.Format("{0} headers and {1} rows", _headers.Count, FullMap.Count);
        }

        public string ToString(bool useOriginalHeaderCase)
        {
            var result = new StringBuilder();

            if (_headers != null)
            {
                if (useOriginalHeaderCase)
                    result.Append(string.Join(_delimiter.ToString(), _headersOriginal));
                else
                    result.Append(string.Join(_delimiter.ToString(), _headers));
            }

            foreach (var row in FullMap)
            {
                result.Append("\n");

                var values = (from item in row select item.Value).ToList();
                result.Append(string.Join(_delimiter.ToString(), values));
            }

            return result.ToString();
        }

        /// <summary>
        /// Agilent formated
        /// </summary>
        /// <returns>A string of the data formated for Agilent software.</returns>
        //public string PrintAgilent()
        //{
        //    var headers = "###Formula\tMass\tCompound name\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n"
        //                  + "#Formula\tMass\tCpd\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n";
        //    string result = headers;
        //    var adduct = new Dictionary<string, Dictionary<string, string>>()
        //    {
        //        { "mPlusHCCS", new Dictionary<string, string>(){
        //            {"polarity","positive"},
        //            {"display","(M+H)+"}
        //        }},
        //        {"mPlusNaCCS", new Dictionary<string, string>(){
        //            { "polarity","positive"},
        //            {"display","(M+Na)+"}
        //        }},
        //        {"mMinusHCCS", new Dictionary<string, string>(){
        //            {"polarity","negative"},
        //            { "display","(M-H)-"}
        //        }},
        //        {"mPlusDotCCS", new Dictionary<string, string>(){
        //            { "polarity","positive"},
        //            {"display","(M)+"}
        //        }}
        //    };
        //    foreach (var row in _full)
        //    {
        //        var tempStr = row[_headerInverse["formula"]]+"\t"
        //            + row[_headerInverse["mass"]] + "\t"
        //            + row[_headerInverse["Neutral Name"]] + "\t"
        //                 + row[_headerInverse["KEGG"]] + "\t"
        //                 + row[_headerInverse["cas"]];
        //        foreach (var key in adduct.Keys)
        //        {
        //            if (!String.IsNullOrEmpty(row[_headerInverse[key]]) && !row[_headerInverse[key]].Equals("N/A"))
        //            {
        //                result += tempStr + "\t" + adduct[key]["polarity"] + "\t" + adduct[key]["display"] + "\t" +
        //                          row[_headerInverse[key]]+"\t\tN2\t\t\n";
        //            }
        //        }

        //    }
        //    return result;
        //}
        public string PrintAgilent()
        {
            var headers = "###Formula\tMass\tCompound name\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n"
                          + "#Formula\tMass\tCpd\tKEGG\tCAS\tPolarity\tIon Species\tCCS\tZ\tGas\tCCS Standard\tNotes\n";
            string result = headers;
            var adduct = new Dictionary<string, Dictionary<string, string>>()
            {
                { "mPlusHCCS", new Dictionary<string, string>(){
                    {"polarity","positive"},
                    {"display","(M+H)+"}
                }},
                {"mPlusNaCCS", new Dictionary<string, string>(){
                    { "polarity","positive"},
                    {"display","(M+Na)+"}
                }},
                {"mMinusHCCS", new Dictionary<string, string>(){
                    {"polarity","negative"},
                    { "display","(M-H)-"}
                }},
                //{"mPlusDotCCS", new Dictionary<string, string>(){
                //    { "polarity","positive"},
                //    {"display","(M)+"}
                //}}
            };
            foreach (var row in FullMap)
            {
                var tempStr = row["formula"] + "\t"
                    + row["mass"] + "\t"
                    + row["neutral name"] + "\t"
                         + row["kegg"] + "\t"
                         + row["cas"];
                foreach (var key in adduct.Keys)
                {
                    if (!String.IsNullOrEmpty(row[key.ToLower()]) && !row[key.ToLower()].Equals("N/A"))
                    {
                        result += tempStr + "\t" + adduct[key]["polarity"] + "\t" + adduct[key]["display"] + "\t" +
                                  row[key.ToLower()] + "\t\tN2\t\t\n";
                    }
                }

            }
            return result;
        }
    }
}
