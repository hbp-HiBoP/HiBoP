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
    }
}