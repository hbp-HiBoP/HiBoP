using Newtonsoft.Json;
using System;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class BuildInfo
    {
        [JsonProperty("Version")] public string Version { get; set; }

        [JsonProperty("UnityVersion")] public string UnityVersion { get; set; }

        [JsonProperty("BuildDate")] public DateTime BuildDate { get; set; }

        public BuildInfo()
        {
            Version = string.Empty;
            UnityVersion = string.Empty;
            BuildDate = DateTime.MinValue;
        }

        public BuildInfo(string version, string unityVersion, DateTime buildDate)
        {
            Version = version;
            UnityVersion = unityVersion;
            BuildDate = buildDate;
        }
    }
}
