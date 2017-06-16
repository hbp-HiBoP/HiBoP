using System.Runtime.Serialization; 

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Connectivity
    {
        const string EXTENSION = "";
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
    }
}
