using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace HBP
{
    [DataContract]
    public class VersionInfo
    {
        [DataMember(Name = "tag_name")]
        public string VersionNumber { get; set; }

        [DataMember(Name = "html_url")]
        public string URL { get; set; }
    }
}