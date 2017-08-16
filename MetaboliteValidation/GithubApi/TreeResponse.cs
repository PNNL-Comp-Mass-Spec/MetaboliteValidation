namespace MetaboliteValidation.GithubApi
{
    /// <summary>
    /// Json response from github to get a list of files
    /// </summary>
    class TreeResponse
    {
        public string sha { get; set; }
        public string url { get; set; }
        public FileInfo[] tree { get; set; }
        public bool truncated { get; set; }
    }
}
