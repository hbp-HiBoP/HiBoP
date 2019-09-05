using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class SingleMesh : Mesh
    {
        #region Properties
        [DataMember(Order = 1, Name = "Path")] public string SavedPath { get; protected set; }
        public string Path
        {
            get
            {
                return SavedPath.ConvertToFullPath();
            }
            set
            {
                SavedPath = value.ConvertToShortPath();
            }
        }
        [DataMember(Order = 2, Name = "MarsAtlasPath")] public string SavedMarsAtlasPath { get; protected set; }
        public string MarsAtlasPath
        {
            get
            {
                return SavedMarsAtlasPath.ConvertToFullPath();
            }
            set
            {
                SavedMarsAtlasPath = value.ConvertToShortPath();
            }
        }
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(Path) && File.Exists(Path) && new FileInfo(Path).Extension == MESH_EXTENSION;
            }
        }
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlasPath) && File.Exists(MarsAtlasPath) && new FileInfo(MarsAtlasPath).Extension == MESH_EXTENSION;
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
            return new SingleMesh(Name, Transformation, ID, Path, MarsAtlasPath);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is SingleMesh singleMesh)
            {
                Path = singleMesh.Path;
                MarsAtlasPath = singleMesh.MarsAtlasPath;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserializedOperation(StreamingContext context)
        {
            SavedPath = SavedPath.ToPath();
            SavedMarsAtlasPath = SavedMarsAtlasPath.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}