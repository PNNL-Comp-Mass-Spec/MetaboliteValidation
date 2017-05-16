using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation
{
    class Program
    {

        private static string Usage(string programName)
        {
            return "Usage: " + programName + " <filename>";
        }
        public static void Main(string[] args)
        {
            // check args
            if(args.Length != 1)
            {
                Console.WriteLine(Usage(AppDomain.CurrentDomain.GetAssemblies().First().FullName));
            }
            // read tsv file
            DelimitedFileParser parser = null;
            try
            {
                parser = new DelimitedFileParser(args[0], '\t');
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            if (parser == null)
                return;
            // get comparison data from pubchem website
            var cids = parser.GetCol("cid");
            var pubchem = new PubchemUtil(cids);
            // compare tsv with pubchem data
            var validator = new ValidatePubchem(pubchem.PubChemMap, parser);
            // report errors or create adgilent file formated data
            var file = new StreamWriter("errorLog.txt");
            file.Write(validator.PrintError());
            file.Close();
            // exit program
            Console.WriteLine("Finished");
        }
    }
}
