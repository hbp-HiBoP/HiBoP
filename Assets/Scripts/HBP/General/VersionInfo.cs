using System.Runtime.Serialization;

namespace HBP
{
    [DataContract]
    public class VersionInfo
    {
        [DataMember(Name = "tag_name")]
        public string VersionNumber { get; set; }

        [DataMember(Name = "html_url")]
        public string URL { get; set; }

        [DataMember(Name = "body")]
        public string Description { get; set; }
    }
}