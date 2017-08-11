using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace metaboliteValidation.GithubApi
{
    /**
     * <summary>This class is for github api interaction</summary>
     */
    public class Github
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Repo { get; set; }
        public string Owner { get; set; }
        public string Branch { get; set; }
        private string githubApiBase = "https://api.github.com";
        private const string ApplicationJson = "application/json";
        protected const string GithubV3Accept = "application/vnd.github.v3.raw+json";
        public Github(string repo, string owner, string branch= "master")
        {
            Owner = owner;
            Repo = repo;
            Branch = branch;
        }
        /**
         * <summary>This function will get the file as a string</summary>
         * <param name="urlBase">The base url for the file</param>
         * <param name="path">The path to the file</param>
         * <returns>The contents of the http request</return>
         */
        public string GetFile(string path)
        {
            if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password))
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
            catch (Exception e)
            {
                Console.WriteLine("Error occured when sending file to github.");
            }
            return null;

        }
        /**
         *  <summary>This function uses the console to collect the username and password for github from the user</summary>
         */
        private void GetUserPass()
        {
            Console.Write("Username: ");
            Username = GetUserName();
            Console.Write("Password: ");
            IntPtr valuePtr = IntPtr.Zero;
            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(GetPassword());
            Password = Marshal.PtrToStringUni(valuePtr);
        }
        /**
         * <summary>This fucntion uses the console to get the user name for github</summary>
         */
        public string GetUserName()
        {
            return Console.ReadLine();
        }
        /**
         * <summary>This function will use the console to get the password to github</summary>
         */
        public SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
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
        /**
         * <summary>This function uses the username and password to create the authentication header to send to github</summary>
         * <returns>The basic athentication header for accessing github</returns>
         */
        private string AuthHeaders()
        {
            return "Basic " +  Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{Username}:{Password}")
                );
         }
        /**
        * <summary>This function will retrieve the sha of a file if the file exists in github</summary>
        * <param name="repo">The github repository</param>
        * <param name="owner">The owner of the github repository</param>
        * <param name="path">The relative path of the file in the repository</param>
        * <returns>The sha of the file or null if it don't exist</returns>
        */
        private string GetSha(string repo, string owner, string path, string branch = "master")
        {
            var fileList = GetFileList(repo, owner, branch);
            if (fileList!= null && fileList.ContainsKey(path))
            {
                return fileList[path].sha;
            }
            return null;
        }
        /**
         * <summary>This function will get a file list from github</summary>
         * <param name="repo">The github repository name</param>
         * <param name="owner">The owner of the github repository</param>
         * <param name="branch">The branch of the github repository</param>
         */
        private Dictionary<string, FileInfo> GetFileList(string repo, string owner, string branch)
        {
            try
            {
                var json = GetJson(GetUriForTree(repo, owner, branch));
                json.Wait();
                return GetFileListFromJson(json.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured when getting information from github.");
            }
            return null;
        }
        /**
         * <summary>This function creates a dictionry of the file list from a json string</summary>
         * <param name="json">The json string representing the file list</param>
         * <returns>The key is the path to the file, value is the file information provided from github</returns>
         */
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
        /**
         * <summary>This function creates a Uri for github api contents</summary>
         */
        private Uri GetUriForContent(string repo, string owner, string path)
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/contents/{path}";
            return new Uri(uriStr);
        }
        /**
         * <summary>This function creates a Uri for github api blob</summary>
         */
        private Uri GetUriForBlob(string repo, string owner, string sha)
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/git/blobs/{sha}";
            return new Uri(uriStr);
        }
        /**
         * <summary>This function creates a Uri for github api tree</summary>
         */
        private Uri GetUriForTree(string repo, string owner, string branch = "master")
        {
            var uriStr = $"{githubApiBase}/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
            return new Uri(uriStr);
        }
        /**
         * <summary>This function uploads a file to github and will update the file or create a new one if the file doesn't exist</summary>
         * <param name="content">The contents of the file to send to github</param>
         * <param name="path">The relative path for the file in github repository</param>
         */
        public void SendFileAsync(string content, string path, string commitMsg = "Updated data", string branch = "master")
        {
            if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password))
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
            catch (Exception e)
            {
                Console.WriteLine("Error occured when sending file to github.");
            }
        }
        /**
         * <summary>This function collects the upload file parameters required for github and serializes them into a json string</summary>
         * <param name="content"> The content to send</param>
         * <param name="sha"> The sha of the file on github</param>
         * <param name="path"> The path to the file on github</param>
         * <param name="message"> The commit message</param>
         * <param name="branch"> The branch of the repository</param>
         * <returns>Json string of the parameters required by github for updating and creating a file if no sha the file will be created on github</returns>
         */
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
        /**
         * <summary>This function sends a PUT http request with the uri and gets a json string back</summary>
         * <param name="uri"> The uri for the http call</param>
         * <param name="content"> The content to send to the uri</param>
         * <returns>The content of the http response as json string</returns>
         */
        private async Task<string> PutJson(Uri uri, StringContent content)
        {
            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Put, uri))
                {
                    req.Headers.Add("Authorization", AuthHeaders());
                    req.Headers.Add("Accept", ApplicationJson);
                    client.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    req.Content = content;
                    using (HttpResponseMessage resp = await client.SendAsync(req))
                    {
                        try
                        {
                            resp.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine("Unauthorized, username, password is incorrect or you don't have access to this repository.");
                            }
                        }
                        return resp.Content.ReadAsStringAsync().Result;
                    }
                }
            }
        }
        /**
         * <summary>This function sends a http GET request with the uri and gets a json string back</summary>
         * <param name="uri">The uri for the http call</param>
         * <returns>The content of the http response as json string</returns>
         */
        private async Task<string> GetJson(Uri uri)
        {
            using (var client = new HttpClient())
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    req.Headers.Add("Authorization", AuthHeaders());
                    req.Headers.Add("Accept", ApplicationJson);
                    client.DefaultRequestHeaders.Add("User-Agent", "Anything");
                    using (HttpResponseMessage resp = await client.SendAsync(req))
                    {
                        try
                        {
                            resp.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                Console.WriteLine("Unauthorized, username, password is incorrect or you don't have access to this repository.");
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
