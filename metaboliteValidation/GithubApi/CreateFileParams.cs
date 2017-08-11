namespace MetaboliteValidation.GithubApi
{
    /// <summary>
    /// Json parameters for creating a file in a repository on github
    /// </summary>
    class CreateFileParams
    {
        public string path { get; set; }
        public string message { get; set; }
        public string content { get; set; }
        public string branch { get; set; }
    }
}
