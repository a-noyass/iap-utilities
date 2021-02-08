namespace Microsoft.IAPUtilities.Definitions.Models.Luis
{
    public class LuisCredentials
    {
        public string Endpoint { get; set; }

        public string Key { get; set; }

        public string AppId { get; set; }

        public LuisCredentials(string endpoint, string key, string appId)
        {
            Endpoint = endpoint;
            Key = key;
            AppId = appId;
        }
    }
}
