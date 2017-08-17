using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using MetaboliteValidation.GithubApi;
using MetaboliteValidation.GoodTableResponse;
using PRISM;
using FileInfo = System.IO.FileInfo;

namespace MetaboliteValidation
{
    class Program
    {

        /// <summary>
        /// This is the url for the goodtables schema located on github
        /// </summary>
        private const string SchemaUrl = "https://raw.githubusercontent.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS/master/metabolitedata-schema.json";

        private const string GOOD_TABLES_WARNING_FILE = "GoodTablesApiOutput.txt";

        private const string DUPLICATE_ROWS_FILE = "DuplicateRows.tsv";

        private const string WARNING_ROWS_FILE = "WarningFile.tsv";

        private const string MISSING_KEGG_FILE = "NoKeggFile.tsv";

        /// <summary>
        /// Name of the master metabolite file on GitHub
        /// </summary>
        /// <remarks>Resides at https://github.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS/blob/master/data/metabolitedata.tsv </remarks>
        private const string MASTER_TSV_FILE = "metabolitedata.tsv";

        /// <summary>
        /// The main function to run the program
        /// </summary>
        /// <param name="args">Passed in arguments to the program</param>
        public static int Main(string[] args)
        {
            var asmName = typeof(Program).GetTypeInfo().Assembly.GetName();
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);       // Alternatively: System.AppDomain.CurrentDomain.FriendlyName
            var version = MetaboliteValidatorOptions.GetAppVersion();

            var parser = new CommandLineParser<MetaboliteValidatorOptions>(asmName.Name, version)
            {
                ProgramInfo = "This program reads metabolites in a .tsv file and pushes new information " + Environment.NewLine +
                              "to the git respository at https://github.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS",

                ContactInfo = "Program written by Ryan Wilson and Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2017" +
                              Environment.NewLine + Environment.NewLine +
                              "E-mail: ryan.wilson@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                              "Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/",

                UsageExamples = {
                    exeName + " NewMetabolites.tsv",
                    exeName + " NewMetabolites.tsv -i",
                    exeName + " NewMetabolites.tsv -preview",
                    exeName + " NewMetabolites.tsv -user MyUsername -password *Dfw3gf"
                }
            };

            var parseResults = parser.ParseArgs(args);
            var options = parseResults.ParsedResults;

            try
            {
                if (!parseResults.Success)
                {
                    System.Threading.Thread.Sleep(1500);
                    return -1;
                }

                if (!options.ValidateArgs())
                {
                    parser.PrintHelp();
                    System.Threading.Thread.Sleep(1500);
                    return -1;
                }

                options.OutputSetOptions();

            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.Write($"Error running {exeName}");
                Console.WriteLine(e.Message);
                Console.WriteLine($"See help with {exeName} --help");
                return -1;
            }

            var program = new Program(parseResults.ParsedResults);

            System.Threading.Thread.Sleep(1500);

            return 0;
        }

        /// <summary>
        /// Construnctor
        /// </summary>
        /// <param name="options">Processing options</param>
        public Program(MetaboliteValidatorOptions options)
        {
            var success = ProcessMetabolites(options);

            Console.WriteLine();

            if (success)
                Console.WriteLine("Processing complete");
            else
                Console.WriteLine("Processing failed");

        }

        /// <summary>
        /// Initialization function that controls the program
        /// </summary>
        /// <param name="options">Processing options</param>
        /// <returns>True on success, false if an error</returns>
        private bool ProcessMetabolites(MetaboliteValidatorOptions options)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(options.InputFile))
                {
                    Console.WriteLine();
                    Console.WriteLine("Error, input file not defined");
                    return false;
                }

                var inputFile = new FileInfo(options.InputFile);

                if (!inputFile.Exists)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error, input file not found: " + inputFile.FullName);
                    return false;
                }

                // init github api interaction with the repo and owner
                var github = new Github("MetabolomicsCCS", "PNNL-Comp-Mass-Spec", options.Preview);

                if (!string.IsNullOrEmpty(options.Username))
                {
                    github.Username = options.Username;

                    if (!string.IsNullOrEmpty(options.Password))
                    {
                        if (options.Password.StartsWith("*"))
                        {
                            github.Password = MetaboliteValidatorOptions.DecodePassword(options.Password.Substring(1));
                        }
                        else
                        {
                            github.Password = options.Password;
                        }

                    }
                }

                // get main data file from github
                var dataFile = github.GetFile("data/" + MASTER_TSV_FILE);

                // parse the new data to append to current data
                var fileToAppend = new DelimitedFileParser();
                fileToAppend.ParseFile(inputFile.FullName, '\t');

                Console.WriteLine();
                Console.WriteLine("Found {0} records in local file {1}", fileToAppend.Count(), inputFile.Name);

                // Update column names if necessary
                UpdateHeaders(fileToAppend);

                // parse the main data file from github
                var mainFile = new DelimitedFileParser();
                if (dataFile == null)
                {
                    mainFile.SetDelimiter('\t');
                    mainFile.SetHeaders(fileToAppend.GetHeaders());
                }
                else
                {
                    mainFile.ParseString(dataFile, '\t');

                    Console.WriteLine();
                    Console.WriteLine("Found {0} records in file {1} retrieved from GitHub", mainFile.Count(), MASTER_TSV_FILE);
                    Console.WriteLine();
                }

                // Update column names if necessary
                UpdateHeaders(mainFile);

                var duplicateRowCount = 0;

                if (!options.IgnoreErrors)
                {
                    // get ids for kegg and pubchem
                    var keggIds = fileToAppend.GetColumnAt("kegg").Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var cidIds = fileToAppend.GetColumnAt("pubchem cid").Where(x => !string.IsNullOrEmpty(x)).ToList();
                    var mainCasIds = mainFile.GetColumnAt("cas").Where(x => !string.IsNullOrEmpty(x)).ToList();

                    // generate pubchem and kegg utils
                    var pub = new PubchemUtil(cidIds.ToArray());
                    var kegg = new KeggUtil(keggIds.ToArray());
                    var file = new StreamWriter("ValidationApi.txt");

                    var dupRows = new DelimitedFileParser();
                    dupRows.SetHeaders(fileToAppend.GetHeaders());
                    dupRows.SetDelimiter('\t');

                    var warningRows = new DelimitedFileParser();
                    warningRows.SetHeaders(fileToAppend.GetHeaders());
                    warningRows.SetDelimiter('\t');

                    var missingKegg = new DelimitedFileParser();
                    missingKegg.SetHeaders(fileToAppend.GetHeaders());
                    missingKegg.SetDelimiter('\t');

                    var dataMap = fileToAppend.GetMap();

                    // compare fileToAppend to utils
                    for (var i = dataMap.Count - 1; i >= 0; i--)
                    {
                        Compound p = null;
                        CompoundData k = null;
                        if (!string.IsNullOrEmpty(dataMap[i]["pubchem cid"]))
                            p = pub.PubChemMap[int.Parse(dataMap[i]["pubchem cid"])];
                        if (!string.IsNullOrEmpty(dataMap[i]["kegg"]) && kegg.CompoundsMap.ContainsKey(dataMap[i]["kegg"]))
                            k = kegg.CompoundsMap[dataMap[i]["kegg"]];
                        if (mainCasIds.Contains(dataMap[i]["cas"]))
                        {
                            dupRows.Add(dataMap[i]);
                            fileToAppend.Remove(dataMap[i]);
                        }
                        else
                        {
                            if (k == null && CheckRow(dataMap[i], p, k))
                            {
                                missingKegg.Add(dataMap[i]);
                            }
                            else if (!CheckRow(dataMap[i], p, k))
                            {
                                // remove from list add to warning file
                                WriteContentToFile(file, dataMap[i], p, k, warningRows.Count() + 2);
                                warningRows.Add(dataMap[i]);
                                fileToAppend.Remove(dataMap[i]);
                            }
                        }
                    }

                    duplicateRowCount = dupRows.Count();

                    file.Close();

                    if (fileToAppend.Count() > 0)
                    {

                        Console.WriteLine("Validating data file with GoodTables");
                        var goodtables = new GoodTables(fileToAppend.ToString(true), SchemaUrl);
                        if (!goodtables.Response.success)
                        {
                            //foreach(var result in goodtables.Response.report.results)
                            //{
                            //    fileToAppend.Remove(result["0"].result_context[0]);
                            //}

                            goodtables.OutputResponse(new StreamWriter(GOOD_TABLES_WARNING_FILE));

                            Console.WriteLine();
                            Console.WriteLine("GoodTables reports errors; see " + GOOD_TABLES_WARNING_FILE);
                            Console.WriteLine("Note that data with N/A in columns that expect a number will be flagged as an error by GoodTables; those errors can be ignored");
                        }
                    }

                    streamToFile(DUPLICATE_ROWS_FILE, dupRows);
                    streamToFile(WARNING_ROWS_FILE, warningRows);
                    streamToFile(MISSING_KEGG_FILE, missingKegg);

                    if (warningRows.Count() > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Warnings were encountered; see file " + WARNING_ROWS_FILE);
                    }

                    if (missingKegg.Count() > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Warnings were encountered; see file " + MISSING_KEGG_FILE);
                    }

                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Ignoring validation, skipping to file upload.");
                }

                if (fileToAppend.Count() == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("No new compounds were found; see {0} for the {1} skipped compounds", DUPLICATE_ROWS_FILE, duplicateRowCount);
                }
                else
                {
                    // this will add the new data tsv to the existing tsv downloaded from github
                    var success = mainFile.Concat(fileToAppend);

                    if (!success)
                    {
                        // Concatenation of new records failed; do not upload
                        return false;
                    }

                    // start command line process for goodtables
                    //
                    // string userDirPath = Environment.GetEnvironmentVariable("goodtables_path");
                    // string commandLine = $"schema \"{options.InputFile}\" --schema \"{SchemaUrl}\"";
                    // string goodtablesPath = $"{userDirPath}\\goodtables";
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
                    github.SendFileAsync(mainFile.ToString(true), "data/" + MASTER_TSV_FILE);

                    // send Agilent file to github
                    github.SendFileAsync(mainFile.PrintAgilent(), "data/metabolitedataAgilent.tsv");
                    //}
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error processing data: " + ex.Message);
                Console.WriteLine(clsStackTraceFormatter.GetExceptionStackTraceMultiLine(ex));
                return false;
            }
        }

        private void streamToFile(string fileName, DelimitedFileParser parsedFile)
        {
            var warnFile = new StreamWriter(fileName);
            warnFile.Write(parsedFile.ToString(true));
            warnFile.Close();
        }

        public bool CheckRow(Dictionary<string, string> row, Compound pubChem, CompoundData kegg)
        {
            var rowFormula = row["formula"];
            var rowCas = row["cas"];
            var rowMass = (int)double.Parse(row["mass"]);
            var pubFormula = "";
            var pubMass = 0.0;
            if (pubChem != null)
            {
                pubFormula = pubChem.findProp("Molecular Formula").sval;
                pubMass = pubChem.findProp("MonoIsotopic").fval;
            }
            if (kegg != null)
            {
                var keggFormula = kegg.Formula;
                var keggExactMass = kegg.ExactMass;
                var keggCas = kegg.OtherId("CAS");
                return rowFormula == keggFormula
                    && rowFormula == pubFormula
                    && rowCas == keggCas
                    && rowMass == (int)keggExactMass
                    && rowMass == (int)pubMass;
            }
            return rowFormula == pubFormula
                && rowMass == (int)pubMass;
        }

        private void UpdateHeaders(DelimitedFileParser fileToAppend)
        {
            var currentHeaders = fileToAppend.GetHeaders();

            // Dictionary mapping old header names to new header names
            var headerMapping = new Dictionary<string, string>();

            foreach (var header in currentHeaders)
            {
                switch (header.ToLower())
                {
                    case "cid":
                        headerMapping.Add(header, "PubChem CID");
                        break;
                }
            }

            if (headerMapping.Count > 0)
            {
                fileToAppend.UpdateHeaders(headerMapping);
            }

        }

        private void WriteContentToFile(TextWriter file, IReadOnlyDictionary<string, string> row, Compound pubChem, CompoundData kegg, int rowIndex)
        {
            file.Write(printHead(rowIndex));
            file.Write(printRow(row));
            file.Write(printKegg(kegg));
            file.Write(printPubChem(pubChem));
            file.Write("\n");
        }

        private string printRow(IReadOnlyDictionary<string, string> a)
        {
            return $"{"Actual",10}{"",10}{(int)double.Parse(a["mass"]),20}{a["formula"],20}{a["cas"],20}\n";
        }

        private string printPubChem(Compound p)
        {
            if (p != null)
                return $"{"PubChem",10}{p.getId(),10}" +
                    $"{(int)p.findProp("MonoIsotopic").fval,20}" +
                    $"{p.findProp("Molecular Formula").sval,20}{"No Cas Information",20}\n";
            return "No PubChem\n";
        }

        private string printKegg(CompoundData k)
        {
            if (k != null)
                return $"{"KEGG",10}{k.KeggId,10}" +
                    $"{(int)k.ExactMass,20}" +
                    $"{k.Formula,20}{k.OtherId("CAS"),20}\n";
            return "No Kegg\n";
        }

        private string printHead(int rowIndex)
        {
            return $"{$"Row {rowIndex}",10}{"ID",10}{"Mass",20}{"Formula",20}{"CAS",20}\n";
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
            var process = new Process();
            // apply all required elements for process
            var startInfo = new ProcessStartInfo(fileName, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
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
            catch (Exception ex)
            {
                Console.WriteLine("Error starting the process: " + ex.Message);
                Console.WriteLine(clsStackTraceFormatter.GetExceptionStackTraceMultiLine(ex));

                // exception from process starting
                Status = StatusCode.FileNotFound;
            }

        }
    }
}
