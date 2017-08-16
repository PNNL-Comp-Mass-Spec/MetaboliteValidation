namespace MetaboliteValidation.GithubApi
{
    /**
    * json parameters for updating a file in a repository on github
    */
    class UploadFileParams : CreateFileParams
    {
        public string sha { get; set; }
    }
}
