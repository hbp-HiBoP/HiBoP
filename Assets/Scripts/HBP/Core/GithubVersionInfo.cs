using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class GithubVersionInfo
    {
        [JsonProperty("tag_name")] public string VersionNumber { get; set; }

        [JsonProperty("html_url")] public string URL { get; set; }

        [JsonProperty("body")] public string Description { get; set; }

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
