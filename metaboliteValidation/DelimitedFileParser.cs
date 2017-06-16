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
        private string[] _headers;
        private string[][] _full;
        private string[][] _reverse;
        private int _columnLength;
        private int _rowLength;
        private char _delimiter;
        private readonly Dictionary<string, int> _headerInverse = new Dictionary<string, int>();
        public DelimitedFileParser() 
        {
            
            
        }
        /**
         * <summary>This function will parse a dilimited string</summary>
         * <param name="content">The delimited string</param>
         * <param name="delimiter">The character used for dilimiting the string</param>
         * <param name="header">Boolean if the first row is a header</param>
         */
        public void ParseString(string content, char delimiter = ',', bool header = true)
        {
            this._delimiter = delimiter;
            Parse(content, delimiter, header);
        }
        /**
         * <summary>This function will parse a dilimited file by reading the file to a string</summary>
         * <param name="content">The path to the delimited file</param>
         * <param name="delimiter">The character used for dilimiting the string</param>
         * <param name="header">Boolean if the first row is a header</param>
         */
        public void ParseFile(string fileName, char delimiter = ',', bool header = true)
        {
            // find file
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }
            this._delimiter = delimiter;
            string content = File.ReadAllText(fileName);
            Parse(content, delimiter, header);
        }
        /**
         * <summary>Private function will parse a dilimited string</summary>
         * <param name="content">The delimited string</param>
         * <param name="delimiter">The character used for dilimiting the string</param>
         * <param name="header">Boolean if the first row is a header</param>
         */
        private void Parse(string content, char delimiter, bool header)
        {
            // remove return carrage symbol
            content = content.Replace("\r", String.Empty);
            // parse file
            var lines = content.Split('\n');
            _rowLength = lines.Length;
            // track column types?

            // if header. track headers in dilimited file
            if (header)
            {
                _headers = lines[0].Split(this._delimiter);
                var temp = new List<string>(lines);
                temp.RemoveAt(0);
                lines = temp.ToArray();
                _rowLength--;
                for (var i = 0; i < _headers.Length; i++)
                {
                    _headerInverse.Add(_headers[i], i);
                }
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
        public Dictionary<string, int> GetHeaderMap()
        {
            return this._headerInverse;
        }
        public string[] GetRow(int index)
        {
            if (index < 0 || index >= _full.Length)
                throw new IndexOutOfRangeException();
            return _full[index];
        }
        public string[] GetCol(int index)
        {
            if (index < 0 || index >= _reverse.Length)
                throw new IndexOutOfRangeException();
            return _reverse[index];
        }
        public string[] GetCol(string colName)
        {
            if(_headerInverse.ContainsKey(colName))
                return _reverse[_headerInverse[colName]];
            return null;
        }
        public string[] GetHeaders()
        {
            return _headers;
        }
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
        public string[][] GetRows()
        {
            return _full;
        }
        /**
         * <summary>This function will compare two string arrays presuming they are the headers</summary>
         * <param name="a">The left side of the compare</param>
         * <param name="b">The right side of the compare</param>
         * <returns>If the two string arrays are the same returns true, false otherwise.</returns>
         */
        private bool CompareHeaders(string[] a, string[] b)
        {
            if (a.Length != b.Length)
                return false;
            for(var i =0;i<a.Length;i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
        /**
         * <summary>This function will append a DelimitedFileParser to the end of this class</summary>
         * <param name="a">The DelimietedFileParser to add to this class</param>
         */
        public bool Concat(DelimitedFileParser a)
        {
            if (CompareHeaders(a.GetHeaders(), _headers))
            {
                var replacement = new string[_full.Length+a.GetRows().Length][];
                var i = 0;
                for (i = 0; i < _full.Length;i++)
                {
                    replacement[i] = _full[i];
                }
                for (var j = 0; j < a.GetRows().Length; j++)
                {
                    replacement[i + j] = a.GetRows()[j];
                }
                _full = replacement;
                return true;
            }
            return false;            
        }
        /**
         * <summary>Converts to a dilimited string using the provided delimiter</summary>
         * <returns>A dilimited string</returns>
         */
        public override string ToString()
        {
            var result = "";
            var firstHead = true;
            foreach (var head in _headers)
            {
                if (!firstHead)
                    result += _delimiter;
                firstHead = false;
                result += head;
            }
            foreach (var row in _full)
            {
                if (!firstHead)
                    result += "\n";
                firstHead = false;
                var firstCol = true;
                foreach (var col in row)
                {
                    if (!firstCol)
                        result += _delimiter;
                    firstCol = false;
                    result += col;
                }
            }
            return result;
        }
        /// <summary>
        /// Agelent formated
        /// </summary>
        /// <returns>A string of the data formated for agelent software.</returns>
        public string PrintAgelent()
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
                {"mPlusDotCCS", new Dictionary<string, string>(){
                    { "polarity","positive"},
                    {"display","(M)+"}
                }}
            };
            foreach (var row in _full)
            {
                var tempStr = row[_headerInverse["formula"]]+"\t"
                    + row[_headerInverse["mass"]] + "\t"
                    + row[_headerInverse["Neutral Name"]] + "\t"
                         + row[_headerInverse["kegg"]] + "\t"
                         + row[_headerInverse["cas"]];
                foreach (var key in adduct.Keys)
                {
                    if (!String.IsNullOrEmpty(row[_headerInverse[key]]) && !row[_headerInverse[key]].Equals("N/A"))
                    {
                        result += tempStr + "\t" + adduct[key]["polarity"] + "\t" + adduct[key]["display"] + "\t" +
                                  row[_headerInverse[key]]+"\t\tN2\t\t\n";
                    }  
                }
                
            }
            return result;
        }
    }
}
