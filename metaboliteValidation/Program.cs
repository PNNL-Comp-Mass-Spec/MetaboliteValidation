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

namespace metaboliteValidation
{
    class Program
    {
        /// <summary>
        /// This function is to describe the usage for the program
        /// </summary>
        /// <param name="programName">The programs name</param>
        /// <returns>The usage from the program</returns>
        private static string Usage(string programName)
        {
            return $"Usage: {programName} <filename>";
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
            // check args
            if(args.Length != 1)
            {
                Console.WriteLine(Usage(AppDomain.CurrentDomain.GetAssemblies().First().FullName));
            }
            new Program(args);
            // exit program
            Console.WriteLine("Finished");
            Console.ReadKey();
        }
        /// <summary>
        /// Construnctor
        /// </summary>
        /// <param name="args">The arguments passed in to the program</param>
        public Program(string[] args)
        {
            Init(args);
        }
        /// <summary>
        /// Initialization function that controls the program
        /// </summary>
        /// <param name="args">The arguments passed in to the program</param>
        private void Init(string[] args)
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
            // this will add the new data tsv to the existing tsv downloaded from github
            mainFile.Concat(fileToAppend);
            GoodTables goodtables = new GoodTables(mainFile.ToString(), SchemaUrl);
            if (goodtables.Response.success) { }
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
