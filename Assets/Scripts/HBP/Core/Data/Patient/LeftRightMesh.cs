using HBP.Core.Tools;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    /// <summary>
    /// A class which contains all the data about a mesh with a file per hemisphere.
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
    /// <term><b>LeftHemisphere</b></term>
    /// <description>Left hemisphere mesh file</description>
    /// </item>
    /// <item>
    /// <term><b>RightHemisphere</b></term>
    /// <description>Right hemisphere mesh file</description>
    /// </item>
    /// <item>
    /// <term><b>LeftMarsAtlasHemisphere</b></term>
    /// <description>Left hemisphere MarsAtlas file</description>
    /// </item>
    /// <item>
    /// <term><b>RightMarsAtlasHemisphere</b></term>
    /// <description>Right hemisphere MarsAtlas file</description>
    /// </item>
    /// <item>
    /// <term><b>Transformation</b></term> 
    /// <description>Transformation file of the mesh.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("LeftRight")]
    public class LeftRightMesh : BaseMesh
    {
        #region Properties
        /// <summary>
        /// Left hemisphere mesh file path with Alias.
        /// </summary>
        [DataMember(Order = 1, Name = "LeftHemisphere")] public string SavedLeftHemisphere { get; protected set; }
        /// <summary>
        /// Left hemisphere mesh file path without Alias.
        /// </summary>
        public string LeftHemisphere
        {
            get
            {
                return SavedLeftHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedLeftHemisphere = value.ConvertToShortPath();
            }
        }
        /// <summary>
        /// Right hemisphere mesh file path with Alias.
        /// </summary>
        [DataMember(Order = 2, Name = "RightHemisphere")] public string SavedRightHemisphere { get; protected set; }
        /// <summary>
        /// Right hemisphere mesh file path without Alias.
        /// </summary>
        public string RightHemisphere
        {
            get
            {
                return SavedRightHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedRightHemisphere = value.ConvertToShortPath();
            }
        }
        /// <summary>
        /// Left hemisphere MarsAtlas file path with Alias.
        /// </summary>
        [DataMember(Order = 3, Name = "LeftMarsAtlasHemisphere")] public string SavedLeftMarsAtlasHemisphere { get; protected set; }
        /// <summary>
        /// Left hemisphere MarsAtlas file path without Alias.
        /// </summary>
        public string LeftMarsAtlasHemisphere
        {
            get
            {
                return SavedLeftMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedLeftMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        [DataMember(Order = 4, Name = "RightMarsAtlasHemisphere")] public string SavedRightMarsAtlasHemisphere { get; protected set; }
        /// <summary>
        /// Right hemisphere MarsAtlas file.
        /// </summary>
        public string RightMarsAtlasHemisphere
        {
            get
            {
                return SavedRightMarsAtlasHemisphere.ConvertToFullPath();
            }
            set
            {
                SavedRightMarsAtlasHemisphere = value.ConvertToShortPath();
            }
        }
        /// <summary>
        /// True if the mesh has mesh files, False otherwise.
        /// </summary>
        public override bool HasMesh
        {
            get
            {
                return !string.IsNullOrEmpty(LeftHemisphere) && !string.IsNullOrEmpty(RightHemisphere) && File.Exists(LeftHemisphere) && File.Exists(RightHemisphere) && new FileInfo(LeftHemisphere).Extension == MESH_EXTENSION && new FileInfo(RightHemisphere).Extension == MESH_EXTENSION;
            }
        }
        /// <summary>
        /// True if the mesh has MarsAtlas files, False otherwise.
        /// </summary>
        public override bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(LeftMarsAtlasHemisphere) && !string.IsNullOrEmpty(RightMarsAtlasHemisphere) && File.Exists(LeftMarsAtlasHemisphere) && File.Exists(RightMarsAtlasHemisphere) && new FileInfo(LeftMarsAtlasHemisphere).Extension == MESH_EXTENSION && new FileInfo(RightMarsAtlasHemisphere).Extension == MESH_EXTENSION;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new LeftRightMesh instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="transformation">Transformation file.</param>
        /// <param name="leftHemisphere">Left hemisphere mesh file</param>
        /// <param name="rightHemisphere">Right hemisphere mesh file</param>
        /// <param name="leftMarsAtlasHemisphere">Left hemisphere MarsAtlas file</param>
        /// <param name="rightMarsAtlasHemisphere">Right hemisphere MarsAtlas file</param>
        /// <param name="ID">Unique identifier</param>
        public LeftRightMesh(string name, string transformation, string leftHemisphere, string rightHemisphere, string leftMarsAtlasHemisphere, string rightMarsAtlasHemisphere, string ID) : base(name, transformation, ID)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
            LeftMarsAtlasHemisphere = leftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = rightMarsAtlasHemisphere;
            RecalculateUsable();
        }
        /// <summary>
        /// Create a new LeftRightMesh instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="transformation">Transformation file.</param>
        /// <param name="leftHemisphere">Left hemisphere mesh file</param>
        /// <param name="rightHemisphere">Right hemisphere mesh file</param>
        /// <param name="leftMarsAtlasHemisphere">Left hemisphere MarsAtlas file</param>
        /// <param name="rightMarsAtlasHemisphere">Right hemisphere MarsAtlas file</param>
        public LeftRightMesh(string name, string transformation, string leftHemisphere, string rightHemisphere, string leftMarsAtlasHemisphere, string rightMarsAtlasHemisphere) : base(name, transformation)
        {
            LeftHemisphere = leftHemisphere;
            RightHemisphere = rightHemisphere;
            LeftMarsAtlasHemisphere = leftMarsAtlasHemisphere;
            RightMarsAtlasHemisphere = rightMarsAtlasHemisphere;
            RecalculateUsable();
        }
        /// <summary>
        /// Create a new LeftRightMesh instance.
        /// </summary>
        public LeftRightMesh() : this("New mesh", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) { }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new LeftRightMesh(Name, Transformation, LeftHemisphere, RightHemisphere, LeftMarsAtlasHemisphere, RightMarsAtlasHemisphere, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is LeftRightMesh leftRightMesh)
            {
                LeftHemisphere = leftRightMesh.LeftHemisphere;
                RightHemisphere = leftRightMesh.RightHemisphere;
                LeftMarsAtlasHemisphere = leftRightMesh.LeftMarsAtlasHemisphere;
                RightMarsAtlasHemisphere = leftRightMesh.RightMarsAtlasHemisphere;
            }
            if (obj is SingleMesh singleMesh)
            {
                LeftHemisphere = singleMesh.Path;
                LeftMarsAtlasHemisphere = singleMesh.MarsAtlasPath;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            SavedLeftHemisphere = SavedLeftHemisphere.StandardizeToEnvironement();
            SavedRightHemisphere = SavedRightHemisphere.StandardizeToEnvironement();
            SavedLeftMarsAtlasHemisphere = SavedLeftMarsAtlasHemisphere.StandardizeToEnvironement();
            SavedRightMarsAtlasHemisphere = SavedRightMarsAtlasHemisphere.StandardizeToEnvironement();
            base.OnDeserialized();
        }
        #endregion
    }
}