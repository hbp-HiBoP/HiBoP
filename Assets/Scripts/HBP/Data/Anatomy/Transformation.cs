using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Transformation
    {
        const string EXTENSION = ".trm";
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
    }
}