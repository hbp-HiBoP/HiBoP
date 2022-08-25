using HBP.Core.Tools;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    /// <summary>
    /// A class which contains all the data about a mesh with a single file for the two hemispheres.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the mesh.</description>
    /// </item>
    /// <item>
    /// <term><b>Path</b></term>
    /// <description>Mesh file</description>
    /// </item>
    /// <item>
    /// <term><b>MarsAtlasPath</b></term>
    /// <description>MarsAtlas file</description>
    /// </item>
    /// <item>
    /// <term><b>Transformation</b></term> 
    /// <description>Transformation file of the mesh.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Single")]
    public class SingleMesh : BaseMesh
    {
        #region Properties
        /// <summary>
        /// Mesh file path with Alias.
        /// </summary>
        [DataMember(Order = 1, Name = "Path")] public string SavedPath { get; protected set; }
        /// <summary>
        /// Mesh file path without Alias.
        /// </summary>
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
        /// <summary>
        /// MarsAtlas file path with Alias.
        /// </summary>
        [DataMember(Order = 2, Name = "MarsAtlasPath")] public string SavedMarsAtlasPath { get; protected set; }
        /// <summary>
        /// MarsAtlas file path without Alias.
        /// </summary>
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
        /// <summary>
        /// True if the mesh has mesh files, False otherwise.
        /// </summary>
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(Path) && File.Exists(Path) && new FileInfo(Path).Extension == MESH_EXTENSION;
            }
        }
        /// <summary>
        /// True if the mesh has MarsAtlas files, False otherwise.
        /// </summary>
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlasPath) && File.Exists(MarsAtlasPath) && new FileInfo(MarsAtlasPath).Extension == MESH_EXTENSION;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new SingleMesh instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="transformation">Transformation file.</param>
        /// <param name="path">Mesh file</param>
        /// <param name="marsAtlasPath">MarsAtlas file</param>
        /// <param name="ID">Unique identifier</param>
        public SingleMesh(string name, string transformation, string path, string marsAtlasPath, string ID) : base(name, transformation, ID)
        {
            Path = path;
            MarsAtlasPath = marsAtlasPath;
            RecalculateUsable();
        }
        /// <summary>
        /// Create a new SingleMesh instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="transformation">Transformation file.</param>
        /// <param name="path">Mesh file</param>
        /// <param name="marsAtlasPath">MarsAtlas file</param>
        public SingleMesh(string name, string transformation, string path, string marsAtlasPath) : base(name, transformation)
        {
            Path = path;
            MarsAtlasPath = marsAtlasPath;
            RecalculateUsable();
        }
        /// <summary>
        /// Create a new SingleMesh instance.
        /// </summary>
        public SingleMesh() : this("New mesh", string.Empty, string.Empty, string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SingleMesh(Name, Transformation, Path, MarsAtlasPath, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is SingleMesh singleMesh)
            {
                Path = singleMesh.Path;
                MarsAtlasPath = singleMesh.MarsAtlasPath;
            }
            if (obj is LeftRightMesh leftRightMesh)
            {
                Path = leftRightMesh.LeftHemisphere;
                MarsAtlasPath = leftRightMesh.LeftMarsAtlasHemisphere;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            SavedPath = SavedPath.StandardizeToEnvironement();
            SavedMarsAtlasPath = SavedMarsAtlasPath.StandardizeToEnvironement();
            base.OnDeserialized();
        }
        #endregion
    }
}