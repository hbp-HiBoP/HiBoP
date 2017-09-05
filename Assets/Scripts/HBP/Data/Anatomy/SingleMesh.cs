using System;
using System.IO;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class SingleMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1)] public string Path { get; set; }
        [DataMember(Order = 2)] public string MarsAtlasPath { get; set; }
        public override bool isUsable
        {
            get
            {
                return base.isUsable && !string.IsNullOrEmpty(Path) && File.Exists(Path) && new FileInfo(Path).Extension == EXTENSION;
            }
        }
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlasPath) && File.Exists(MarsAtlasPath) && new FileInfo(MarsAtlasPath).Extension == EXTENSION;
            }
        }
        #endregion

        #region Constructors
        public SingleMesh(string name, string transformation, string path,string marsAtlasPath) : base(name, transformation)
        {
            Path = path;
            MarsAtlasPath = marsAtlasPath;
        }
        public SingleMesh() : this("New mesh", string.Empty, string.Empty, string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SingleMesh(Name, Transformation, Path, MarsAtlasPath);
        }
        public override void Copy(object copy)
        {
            SingleMesh mesh = copy as SingleMesh;
            Name = mesh.Name;
            Path = mesh.Path;
            MarsAtlasPath = mesh.MarsAtlasPath;
        }
        #endregion
    }
}