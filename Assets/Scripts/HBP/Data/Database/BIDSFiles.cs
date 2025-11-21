using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Data.BIDS
{
    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class DatasetDescription
    {
        public string Name = "BIDS Dataset";
        public string BIDSVersion = "1.10.1";
        public string DatasetType = "derivative";
        public GeneratedBy[] GeneratedBy = new GeneratedBy[1] { new() };

        public DatasetDescription() { }
        public DatasetDescription(string name)
        {
            Name = name;
        }
    }

    [JsonObject(MemberSerialization.OptOut), Preserve]
    public class GeneratedBy
    {
        public string Name = Application.productName;
        public string Version = Application.version;
        public string CodeURL = "https://github.com/hbp-HiBoP/HiBoP";
    }
}