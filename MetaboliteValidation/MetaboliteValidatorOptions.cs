using System;
using System.Collections.Generic;
using System.Reflection;
using PRISM;

namespace MetaboliteValidation
{
    class MetaboliteValidatorOptions
    {
        private const string PROGRAM_DATE = "May 8, 2019";

        public MetaboliteValidatorOptions()
        {
            IgnoreErrors = false;
            Preview = false;
            Username = string.Empty;
            Password = string.Empty;
        }

        [Option("input", ArgPosition = 1, HelpText = "Input file (tab-delimited file of new metabolites)")]
        public string InputFile { get; set; }

        [Option("i", "ignore",  HelpText = "Ignore validation errors and push new data anyway", HelpShowsDefault = true)]
        public bool IgnoreErrors { get; set; }

        [Option("preview", HelpText = "Preview data that would be sent to GitHub")]
        public bool Preview { get; set; }

        [Option("u", "user", "username", HelpText = "GitHub username")]
        public string Username { get; set; }

        [Option("p", "pass", "password", HelpText = "GitHub password; to indicate that it is a scrambled password, prepend with an asterisk")]
        public string Password { get; set; }

        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        public void OutputSetOptions()
        {
            Console.WriteLine("Using options:");

            if (!string.IsNullOrWhiteSpace(Username))
            {
                Console.WriteLine(" GitHub user: {0}", Username);
                if (!string.IsNullOrWhiteSpace(Password))
                    Console.WriteLine(" GitHub password provided");
            }

            Console.WriteLine(" Reading data from: {0}", InputFile);

            Console.WriteLine(" Ignore Errors: {0}", IgnoreErrors);

            if (Preview)
                Console.WriteLine("Previewing validation actions");
        }

        /// <summary>
        /// Decrypts an encoded password
        /// </summary>
        /// <param name="enPwd">Encoded password</param>
        /// <returns>Clear text password</returns>
        public static string DecodePassword(string enPwd)
        {
            // Convert the password string to a character array
            var pwdChars = enPwd.ToCharArray();
            var pwdBytes = new List<byte>();
            var pwdCharsAdj = new List<char>();

            for (var i = 0; i <= pwdChars.Length - 1; i++)
            {
                pwdBytes.Add((byte)pwdChars[i]);
            }

            // Modify the byte array by shifting alternating bytes up or down and convert back to char, and add to output string

            for (var byteCntr = 0; byteCntr <= pwdBytes.Count - 1; byteCntr++)
            {
                if (byteCntr % 2 == 0)
                {
                    pwdBytes[byteCntr] += 1;
                }
                else
                {
                    pwdBytes[byteCntr] -= 1;
                }
                pwdCharsAdj.Add((char)pwdBytes[byteCntr]);
            }

            return string.Join("", pwdCharsAdj);

        }

        public bool ValidateArgs(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(InputFile))
            {
                errorMessage = "You must specify an input file";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

    }
}
