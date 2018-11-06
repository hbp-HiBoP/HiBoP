using System;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class SingleMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1, Name = "Path")] string m_Path;
        public string Path
        {
            get
            {
                return m_Path.ConvertToFullPath();
            }
            set
            {
                m_Path = value.ConvertToShortPath();
            }
        }
        public string SavedPath { get { return m_Path; } }
        [DataMember(Order = 2, Name = "MarsAtlasPath")] string m_MarsAtlasPath;
        public string MarsAtlasPath
        {
            get
            {
                return m_MarsAtlasPath.ConvertToFullPath();
            }
            set
            {
                m_MarsAtlasPath = value.ConvertToShortPath();
            }
        }
        public string SavedMarsAtlasPath { get { return m_MarsAtlasPath; } }
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(Path) && File.Exists(Path) && new FileInfo(Path).Extension == EXTENSION;
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
        public SingleMesh(string name, string transformation,string ID, string path, string marsAtlasPath) : base(name, transformation,ID)
        {
            Path = path;
            MarsAtlasPath = marsAtlasPath;
            RecalculateUsable();
        }
        public SingleMesh(string name, string transformation, string path,string marsAtlasPath) : base(name, transformation)
        {
            Path = path;
            MarsAtlasPath = marsAtlasPath;
            RecalculateUsable();
        }
        public SingleMesh() : this("New mesh", string.Empty, string.Empty, string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SingleMesh(Name, m_Transformation, ID, m_Path, m_MarsAtlasPath);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);

            SingleMesh mesh = copy as SingleMesh;
            Path = mesh.Path;
            MarsAtlasPath = mesh.MarsAtlasPath;
        }
        #endregion
    }
}