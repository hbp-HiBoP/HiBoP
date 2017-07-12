using System.IO;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class SingleMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1)] public string Path { get; set; }
        public override bool isUsable
        {
            get
            {
                return base.isUsable && !string.IsNullOrEmpty(Path) && new FileInfo(Path).Extension == EXTENSION;
            }
        }
        #endregion

        #region Constructors
        public SingleMesh(string name,string path) : base(name)
        {
            Path = path;
        }
        public SingleMesh() : this("New mesh", string.Empty) { }
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