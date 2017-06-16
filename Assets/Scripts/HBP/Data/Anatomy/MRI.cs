using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class MRI
    {
        const string EXTENSION = ".nii";
        [DataMember] public string Name { get; set; }
        [DataMember] public string Path { get; set; }
    }
}