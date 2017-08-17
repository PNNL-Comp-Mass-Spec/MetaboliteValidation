using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MetaboliteValidation.GithubApi
{
    /// <summary>
    /// This class is for github api interaction
    /// </summary>
    public class Github
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Repo { get; set; }
        public string Owner { get; set; }
        public string Branch { get; set; }
        private readonly string githubApiBase = "https://api.github.com";

        private bool PreviewMode { get; }

        private const string ApplicationJson = "application/json";
        protected const string GithubV3Accept = "application/vnd.github.v3.raw+json";

        public Github(string repo, string owner, bool previewMode = false, string branch= "master")
        {
            Owner = owner;
            Repo = repo;
            Branch = branch;
            PreviewMode = previewMode;
        }

        /// <summary>
        /// This function will get the file as a string
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>The contents of the http request</returns>
        public string GetFile(string path)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                GetUserPass();
            }

            var uri = GetUriForContent(Repo, Owner, path);
            try
            {
                var json = GetJson(uri);
                json.Wait();
                var fileContents = JsonConvert.DeserializeObject<FileContent>(json.Result);
                return fileContents.GetContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving file from GitHub: " + ex.Message);
            }
            return null;

        }

        /// <summary>
        /// This function uses the console to collect the username and password for github from the user
        /// </summary>
        private void GetUserPass()
        {
            Console.WriteLine();

            if (string.IsNullOrEmpty(Username))
            {
                Console.Write("GitHub username: ");
                Username = GetUserName();
                Console.Write("Password: ");
            }
            else
            {
                Console.Write($"Github password for {Username}: ");
            }

            var valuePtr = Marshal.SecureStringToGlobalAllocUnicode(GetPassword());
            Password = Marshal.PtrToStringUni(valuePtr);
        }

        /// <summary>
        /// This fucntion uses the console to get the user name for github
        /// </summary>
        /// <returns></returns>
        public string GetUserName()
        {
            return Console.ReadLine();
        }

        /// <summary>
        /// This function will use the console to get the password to github
        /// </summary>
        /// <returns></returns>
        public SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }
            Console.Write("\n");
            return pwd;
        }

        /// <summary>
        /// This function uses the username and password to create the authentication header to send to GitHub
        /// </summary>
        /// <returns>The basic athentication header for accessing GitHub</returns>
        private string AuthHeaders()
        {
            return "Basic " +  Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{Username}:{Password}")
                );
        }

        /// <summary>
        /// This function will retrieve the sha of a file if the file exists in github
        /// </summary>
        /// <param name="repo">The github repository</param>
        /// <param name="owner">The owner of the github repository</param>
        /// <param name="path">The relative path of the file in the repository</param>
        /// <param name="branch">Optional: branch name</param>
        /// <returns>The sha of the file or null if it don't exist</returns>
        private string GetSha(string repo, string owner, string path, string branch = "master")
        {
            var fileList = GetFileList(repo, owner, branch);
            if (fileList!= null && fileList.ContainsKey(path))
            {
                return fileList[path].sha;
            }
            return null;
        }

        /// <summary>
        /// This function will get a file list from github
        /// </summary>
        /// <param name="repo">The github repository</param>
        /// <param name="owner">The owner of the github repository</param>
        /// <param name="branch">Optional: branch name</param>
        /// <returns></returns>
        private Dictionary<string, FileInfo> GetFileList(string repo, string owner, string branch)
        {
            try
            {
                var json = GetJson(GetUriForTree(repo, owner, branch));
                json.Wait();
                return GetFileListFromJson(json.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting file list from GitHub: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// This function creates a dictionary of the file list from a json string
        /// </summary>
        /// <param name="json">The json string representing the file list</param>
        /// <returns>The key is the path to the file, value is the file information provided from github</returns>
        private Dictionary<string, FileInfo> GetFileListFromJson(string json)
        {
            var result = JsonConvert.DeserializeObject<TreeResponse>(json);
            var results = new Dictionary<string, FileInfo>();
            foreach (var fileInfo in result.tree)
            {
                results.Add(fileInfo.path,fileInfo);
            }
            return results;
        }

        /// <summary>
        /// This function creates a Uri for github api contents
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="owner"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private Uri GetUriForContent(string repo, string owner, string path)
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/contents/{path}";
            return new Uri(uriStr);
        }

        /// <summary>
        /// This function creates a Uri for github api blob
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="owner"></param>
        /// <param name="sha"></param>
        /// <returns></returns>
        private Uri GetUriForBlob(string repo, string owner, string sha)
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/git/blobs/{sha}";
            return new Uri(uriStr);
        }

        /// <summary>
        /// This function creates a Uri for github api tree
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="owner"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        private Uri GetUriForTree(string repo, string owner, string branch = "master")
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
            return new Uri(uriStr);
        }

        /// <summary>
        /// This function uploads a file to github and will update the file or create a new one if the file doesn't exist
        /// </summary>
        /// <param name="content">The contents of the file to send to github</param>
        /// <param name="path">The relative path for the file in github repository</param>
        /// <param name="commitMsg">Commit message</param>
        /// <param name="branch">Optional: branch</param>
        public void SendFileAsync(string content, string path, string commitMsg = "Updated data", string branch = "master")
        {
            const int ROWS_TO_PREVIEW = 5;

            if (PreviewMode)
            {
                Console.WriteLine();
                Console.WriteLine("Preview of data to push to {0} on GitHub", path);

                var contentRows = content.Split('\r', '\n');

                Console.WriteLine("Displaying {0} / {1} total rows", ROWS_TO_PREVIEW, contentRows.Length);

                // Show the first 5 rows
                var rowsPreviewed = 0;
                foreach (var dataRow in contentRows)
                {
                    if (string.IsNullOrWhiteSpace(dataRow))
                        continue;

                    if (dataRow.Length < 80)
                        Console.WriteLine(dataRow);
                    else
                        Console.WriteLine(dataRow.Substring(0, 77) + " ...");

                    rowsPreviewed++;
                    if (rowsPreviewed >= ROWS_TO_PREVIEW)
                        break;
                }

                return;
            }

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                GetUserPass();
            }
            var uri = GetUriForContent(Repo, Owner, path);
            var sha = GetSha(Repo, Owner, path);
            try
            {
                var json = PutJson(uri, UploadFileParams(content, sha, path, commitMsg, branch));
                json.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending file to GitHub: " + ex.Message);
            }
        }

        /// <summary>
        /// This function collects the upload file parameters required for github and serializes them into a json string
        /// </summary>
        /// <param name="content">The content to send</param>
        /// <param name="sha">The sha of the file on github</param>
        /// <param name="path">The path to the file on github</param>
        /// <param name="message">The commit message</param>
        /// <param name="branch">The branch of the repository</param>
        /// <returns>Json string of the parameters required by github for updating and creating a file if no sha the file will be created on github</returns>
        private StringContent UploadFileParams(string content, string sha, string path, string message, string branch)
        {
            var fileParam  = new CreateFileParams
                {
                    branch = branch,
                    content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                    message = message,
                    path = path
                };
            if (sha != null)
            {
                fileParam = new UploadFileParams
                {
                    branch = branch,
                    content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
                    message = message,
                    path = path,
                    sha = sha
                };
            }
            return new StringContent(JsonConvert.SerializeObject(fileParam),Encoding.UTF8, ApplicationJson);
        }

        /// <summary>
        /// This function sends a PUT http request with the uri and gets a json string back
        /// </summary>
        /// <param name="uri">The uri for the http call</param>
        /// <param name="content">The content to send to the uri</param>
        /// <returns>The content of the http response as json string</returns>
        private async Task<string> PutJson(Uri uri, HttpContent content)
        {
            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Put, uri))
                {
                    req.Headers.Add("Authorization", AuthHeaders());
                    req.Headers.Add("Accept", ApplicationJson);
                    client.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    req.Content = content;
                    using (var resp = await client.SendAsync(req))
                    {
                        try
                        {
                            resp.EnsureSuccessStatusCode();
                        }
                        catch (Exception ex)
                        {
                            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine("Unauthorized, username, password is incorrect or you don't have access to this repository.");
                            }
                            else
                            {
                                Console.WriteLine("Error sending content to GitHub: " + ex.Message);
                            }
                        }
                        return resp.Content.ReadAsStringAsync().Result;
                    }
                }
            }
        }

        /// <summary>
        /// This function sends a http GET request with the uri and gets a json string back
        /// </summary>
        /// <param name="uri">The uri for the http call</param>
        /// <returns>The content of the http response as json string</returns>
        private async Task<string> GetJson(Uri uri)
        {
            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    req.Headers.Add("Authorization", AuthHeaders());
                    req.Headers.Add("Accept", ApplicationJson);
                    client.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    using (var resp = await client.SendAsync(req))
                    {
                        try
                        {
                            resp.EnsureSuccessStatusCode();
                        }
                        catch (Exception ex)
                        {
                            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine("Unauthorized, username, password is incorrect or you don't have access to this repository.");
                            }
                            else
                            {
                                Console.WriteLine("Error retrieving content from GitHub: " + ex.Message);
                            }

                            if (resp.StatusCode == HttpStatusCode.NotFound)
                            {
                                return null;
                            }
                        }
                        return resp.Content.ReadAsStringAsync().Result;
                    }

                }
            }
        }
    }
}
