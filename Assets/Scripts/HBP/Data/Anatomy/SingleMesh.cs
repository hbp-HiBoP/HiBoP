
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class SingleMesh : Mesh
    {
        #region Properties
        [DataMember] public string Path { get; set; }
        #endregion

        #region Constructors
        public SingleMesh(string name,string path) : base(name)
        {
            Path = path;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SingleMesh(Name,Path);
        }
        public override void Copy(object copy)
        {
            SingleMesh mesh = copy as SingleMesh;
            Name = mesh.Name;
            Path = mesh.Path;
        }
        #endregion
    }
}