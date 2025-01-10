using System;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public class BuildInfo
    {
        [DataMember(Name = "Version")]
        public string Version { get; set; }

        [DataMember(Name = "UnityVersion")]
        public string UnityVersion { get; set; }

        [DataMember(Name = "BuildDate")]
        public DateTime BuildDate { get; set; }
    }
}