using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GithubVersionInfo
    {
        [JsonProperty("tag_name")]
        public string VersionNumber { get; set; }

        [JsonProperty("html_url")]
        public string URL { get; set; }

        [JsonProperty("body")]
        public string Description { get; set; }

        public GithubVersionInfo()
        {
            VersionNumber = string.Empty;
            URL = string.Empty;
            Description = string.Empty;
        }
        public GithubVersionInfo(string versionNumber, string url, string description)
        {
            VersionNumber = versionNumber;
            URL = url;
            Description = description;
        }
    }
}