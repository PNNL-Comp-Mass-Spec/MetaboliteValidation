using metaboliteValidation.GithubApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using metaboliteValidation.GoodTableResponse;
using NDesk.Options;

namespace metaboliteValidation
{
    class Program
    {
        /// <summary>
        /// This function is to describe the usage for the program
        /// </summary>
        /// <param name="programName">The programs name</param>
        /// <returns>The usage from the program</returns>
        private static void Usage(string programName, OptionSet options)
        {
            Console.Write($"Usage: {programName} [OPTIONS] <filename>\nOptions:\n");
            options.WriteOptionDescriptions(Console.Out);
        }
        /// <summary>
        /// This is the url for the goodtables schema located on github
        /// </summary>
        private const string SchemaUrl = "https://raw.githubusercontent.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS/master/metabolitedata-schema.json";
        /// <summary>
        /// The main function to run the program
        /// </summary>
        /// <param name="args">Passed in arguments to the program</param>
        public static void Main(string[] args)
        {
            bool show_help = false;
            bool ignore = false;
            var options = new OptionSet()
            {
                { "i|ignore", "Ignore warnings and push data to github", v => ignore = v != null },
                { "h|help",  "show this message and exit", v => show_help = v != null }
            };
            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write($"{System.AppDomain.CurrentDomain.FriendlyName}: ");
                Console.WriteLine(e.Message);
                Console.WriteLine($"Try `{System.AppDomain.CurrentDomain.FriendlyName} --help' for more information.");
                return;
            }
            // check args
            if (extra.Count != 1||show_help)
            {
                Usage(System.AppDomain.CurrentDomain.FriendlyName, options);
                return;
            }
            new Program(extra.ToArray(), ignore);
            // exit program
            Console.WriteLine("Finished");
            Console.ReadKey();
        }
        /// <summary>
        /// Construnctor
        /// </summary>
        /// <param name="args">The arguments passed in to the program</param>
        public Program(string[] args,bool ignore)
        {
            Init(args, ignore);
        }
        /// <summary>
        /// Initialization function that controls the program
        /// </summary>
        /// <param name="args">The file path passed in to the program</param>
        /// <param name="ignore">If the program should ignore the validation</param>
        private void Init(string[] args, bool ignore)
        {
            
            // init github api interaction with the repo and owner
            var github = new Github("MetabolomicsCCS", "PNNL-Comp-Mass-Spec");
            // get main data file from github 
            var dataFile = github.GetFile("data/metabolitedata.tsv");
            if (dataFile == null) Environment.Exit(1);
            // strings to run good tables in the command line
            string userDirPath = Environment.GetEnvironmentVariable("goodtables_path");
            string commandLine = $"schema \"{args[0]}\" --schema \"{SchemaUrl}\"";
            string goodtablesPath = $"{userDirPath}\\goodtables";
            // parse the main data file from github
            DelimitedFileParser mainFile = new DelimitedFileParser();
            mainFile.ParseString(dataFile, '\t');
            // parse the new data to append to current data
            DelimitedFileParser fileToAppend = new DelimitedFileParser();
            fileToAppend.ParseFile(args[0], '\t');
            if (!ignore)
            {
                // get ids for kegg and pubchem
                List<string> keggIds = fileToAppend.GetColumnAt("KEGG").Where(x => !string.IsNullOrEmpty(x)).ToList();
                List<string> cidIds = fileToAppend.GetColumnAt("CID").Where(x => !string.IsNullOrEmpty(x)).ToList();
                // generate pubchem and kegg utils
                PubchemUtil pub = new PubchemUtil(cidIds.ToArray());
                KeggUtil kegg = new KeggUtil(keggIds.ToArray());
                StreamWriter file = new StreamWriter("testValidationApi.txt");

                DelimitedFileParser warningRows = new DelimitedFileParser();
                warningRows.SetHeaders(fileToAppend.GetHeaders());
                warningRows.SetDelimiter('\t');
                // compare fileToAppend to utils
                foreach (var a in fileToAppend.GetMap())
                {
                    Compound p = null;
                    CompoundData k = null;
                    if (!string.IsNullOrEmpty(a["CID"]))
                        p = pub.PubChemMap[int.Parse(a["CID"])];
                    if (!string.IsNullOrEmpty(a["KEGG"]))
                        k = kegg.CompoundsMap[a["KEGG"]];
                    if (!CheckRow(a, p, k))
                    {
                        // remove from list add to warning file
                        WriteContentToFile(file, a, p, k);
                        warningRows.Add(a);
                        
                    }
                }
                foreach (var wRow in warningRows.GetMap())
                {
                    fileToAppend.Remove(wRow);
                }

                file.Close();
                GoodTables goodtables = new GoodTables(fileToAppend.ToString(), SchemaUrl);
                if (!goodtables.Response.success) { goodtables.OutputResponse(new StreamWriter("testGoodTablesApi.txt")); }
                StreamWriter warnFile = new StreamWriter("WarningFile.tsv");
                warnFile.Write(warningRows.ToString());
                warnFile.Close();
            }
            else
            {
                Console.WriteLine("Ignoring validation, skipping to file upload.");
            }

            if (fileToAppend.Count() > 0)
            {
                // this will add the new data tsv to the existing tsv downloaded from github
                mainFile.Concat(fileToAppend);

                // start command line process for goodtables
                //CommandLineProcess pro = new CommandLineProcess(goodtablesPath, commandLine);
                //// if error display errors and exit
                //if (pro.Status.Equals(CommandLineProcess.StatusCode.Error))
                //{
                //    Console.WriteLine($"GoodTables Validation error\n\n{pro.StandardOut}{pro.StandardError}\nExiting program please check that the data is valid.");
                //    Console.ReadKey();
                //    Environment.Exit(1);
                //}
                //// if the goodtables.exe file isn't found display message and exit
                //else if (pro.Status.Equals(CommandLineProcess.StatusCode.FileNotFound))
                //{
                //    Console.WriteLine("File not found. Please make sure you have installed python and goodtables.\n"
                //        +"Check that the folder path for goodtables.exe is added to an environment variable named GOODTABLES_PATH.\n"
                //        +"Press any key to continue.");
                //    Console.ReadKey();
                //    Environment.Exit(1);
                //}
                //else
                //{
                //    Console.WriteLine($"GoodTables validation\n\n{pro.StandardOut}");
                //    
                // This will send the completed tsv back to github
                github.SendFileAsync(mainFile.ToString(), "data/dataTest.tsv");
                //    // create agelent file
                // send agelent file to github
                github.SendFileAsync(mainFile.PrintAgilent(), "data/dataAgilentTest.tsv");
                //}
            }
        }
        public bool CheckRow(Dictionary<string, string> row, Compound pubChem, CompoundData kegg)
        {
            var rowFormula = row["formula"];
            var rowCas = row["cas"];
            var rowMass = (int)double.Parse(row["mass"]);
            var pubFormula = "";
            var pubMass = 0.0;
            var keggFormula = "";
            var keggExactMass = 0.0;
            var keggCas = "";
            if (pubChem != null)
            {
                pubFormula = pubChem.findProp("Molecular Formula").sval;
                pubMass = pubChem.findProp("MonoIsotopic").fval;
            }
            if (kegg != null)
            {
                keggFormula = kegg.Formula;
                keggExactMass = kegg.ExactMass;
                keggCas = kegg.OtherIds.Where(x=>x.Key == "CAS").ToList().First().Value;
            }
            return rowFormula == keggFormula
                && rowFormula == pubFormula
                && rowCas == keggCas
                && rowMass == (int)keggExactMass
                && rowMass == (int)pubMass;
        }
        private void WriteContentToFile(StreamWriter file, Dictionary<string, string> row, Compound pubChem, CompoundData kegg)
        {
            file.Write(printHead());
            file.Write(printRow(row));
            file.Write(printKegg(kegg));
            file.Write(printPubChem(pubChem));
            file.Write("\n");
        }
        private string printRow(Dictionary<string, string> a)
        {
            return $"{"Actual",10}{(int)double.Parse(a["mass"]),20}{a["formula"],20}\n";
        }
        private string printPubChem(Compound p)
        {
            if (p != null)
                return $"{"PubChem",10}" +
                    $"{(int)p.findProp("MonoIsotopic").fval,20}" +
                    $"{p.findProp("Molecular Formula").sval,20}\n";
            return "No PubChem\n";
        }
        private string printKegg(CompoundData k)
        {
            if (k != null)
                return $"{"Kegg",10}" +
                    $"{(int)k.ExactMass,20}" +
                    $"{k.Formula,20}\n";
            return "No Kegg\n";
        }
        private string printHead()
        {
            return $"{"",10}{"Mass",20}{"Formula",20}\n";
        }
    }
    
    /// <summary>
    /// Simple class to run a command line process and get more feed back for handling issues
    /// </summary>
    public class CommandLineProcess
    {
        /// <summary>
        /// enum for status codes to make clear which status is which
        /// </summary>
        public enum StatusCode
        {
            Ok,
            Error,
            FileNotFound
        }
        public string StandardOut { get; set; }
        public string StandardError { get; set; }
        public StatusCode Status { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filename">The command line file to run</param>
        /// <param name="args">the parameters to pass to the program</param>
        public CommandLineProcess(string filename, string args)
        {
            // init the status to ok
            Status = StatusCode.Ok;
            Init(filename, args);
        }
        /// <summary>
        /// this function controls the class behavior
        /// </summary>
        /// <param name="fileName">The command line file to run</param>
        /// <param name="args">the parameters to pass to the program</param>
        private void Init(string fileName, string args)
        {
            // create a process
            Process process = new Process();
            // apply all required elements for process
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, args);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            try
            {
                // start the process
                process.Start();
                process.WaitForExit();
                StandardOut = process.StandardOutput.ReadToEnd();
                StandardError = process.StandardError.ReadToEnd();
                // if error set status code
                if (!process.ExitCode.Equals(0)) Status = StatusCode.Error;
            }
            catch (Exception e)
            {
                // exception from process starting
                Status = StatusCode.FileNotFound;
            }
            
        }
    }
}
