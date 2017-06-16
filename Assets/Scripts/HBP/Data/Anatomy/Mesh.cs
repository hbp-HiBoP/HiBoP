using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Mesh
    {
        const string EXTENSION = ".gii";
        [DataMember] public string Name { get; set; }
        [DataMember] public string LeftHemisphere { get; set; }
        [DataMember] public string RightHemisphere { get; set; }
    }
}