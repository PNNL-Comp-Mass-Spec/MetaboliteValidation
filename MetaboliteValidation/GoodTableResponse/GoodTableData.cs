namespace MetaboliteValidation.GoodTableResponse
{
    public class GoodtableData
    {
        public string data { get; set; }
        public string schema { get; set; }
        public GoodtableData(string data, string schema)
        {
            this.data = data;
            this.schema = schema;
        }
    }
}
