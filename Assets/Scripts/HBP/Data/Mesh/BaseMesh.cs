using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Core.Interfaces;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// A base class which contains all the data about a mesh.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the mesh.</description>
    /// </item>
    /// <item>
    /// <term><b>Transformation</b></term> 
    /// <description>Transformation file of the mesh.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class BaseMesh : BaseData, INameable
    {
        #region Properties
        /// <summary>
        /// Extension of mesh files.
        /// </summary>
        public const string MESH_EXTENSION = ".gii";
        /// <summary>
        /// Extension of transformation files.
        /// </summary>
        public const string TRANSFORMATION_EXTENSION = ".trm";
        /// <summary>
        /// Name of the mesh.
        /// </summary>
        [DataMember(Order = 0)] public string Name { get; set; }

        /// <summary>
        /// Specifies if a mesh was usable at the last verification. Don't perform the verification.
        /// </summary>
        public bool WasUsable { get; protected set; }
        /// <summary>
        /// Specifies if a mesh is usable.
        /// </summary>
        public bool IsUsable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasMesh;
                WasUsable = usable;
                return usable;
            }
        }
        /// <summary>
        /// The mesh object has a mesh file.
        /// </summary>
        public virtual bool HasMesh { get { return false; } }
        /// <summary>
        /// The mesh object has a marsAtlas file.
        /// </summary>
        public virtual bool HasMarsAtlas { get { return false; } }
        /// <summary>
        /// The mesh object has a transformation file.
        /// </summary>
        public virtual bool HasTransformation
        {
            get
            {
                return !string.IsNullOrEmpty(Transformation) && File.Exists(Transformation) && (new FileInfo(Transformation).Extension == TRANSFORMATION_EXTENSION || new FileInfo(Transformation).Extension == ".txt");
            }
        }
        [DataMember(Order = 5, Name = "Transformation")] public string SavedTransformation { get; protected set; }
        /// <summary>
        /// Transformation file of the mesh.
        /// </summary>
        public string Transformation
        {
            get
            {
                return SavedTransformation.ConvertToFullPath();
            }
            set
            {
            SavedTransformation = value.ConvertToShortPath();
            }
        }
        #endregion

        #region Constructors 
        /// <summary>
        /// Initializes a new instance of the Mesh class.
        /// </summary>
        /// <param name="name">Name of the mesh.</param>
        /// <param name="transformation">Transformation file of the mesh.</param>
        /// <param name="ID">Unique identifier to identify the mesh.</param>
        public BaseMesh(string name, string transformation, string ID): base(ID)
        {
            Name = name;
            Transformation = transformation;
        }
        /// <summary>
        /// Initializes a new instance of the Mesh class.
        /// </summary>
        /// <param name="name">Name of the mesh.</param>
        /// <param name="patients">Transformation file of the mesh.</param>
        /// <param name="id">Unique identifier to identify the mesh.</param>
        public BaseMesh(string name, string transformation) : base()
        {
            Name = name;
            Transformation = transformation;
        }
        /// <summary>
        /// Initializes a new instance of the Mesh class.
        /// </summary>
        public BaseMesh() : this("New mesh", string.Empty)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Recalculates if the mesh is usable.
        /// </summary>
        /// <returns></returns>
        public bool RecalculateUsable()
        {
            return IsUsable;
        }
        /// <summary>
        /// Loads meshes from a directory.
        /// </summary>
        /// <param name="path">path of the directory.</param>
        /// <returns></returns>
        public static BaseMesh[] LoadFromDirectory(string path)
        {
            List<BaseMesh> meshes = new List<BaseMesh>();
            DirectoryInfo parent = new DirectoryInfo(path);
            DirectoryInfo t1mr1 = new DirectoryInfo(Path.Combine(path, "t1mri"));

            DirectoryInfo preimplantationDirectory = null, preTransformationsDirectory = null;
            FileInfo preTransformation = null;
            preimplantationDirectory = t1mr1.GetDirectories("T1pre_*").FirstOrDefault();
            if (preimplantationDirectory != null && preimplantationDirectory.Exists)
            {
                preTransformationsDirectory = new DirectoryInfo(Path.Combine(preimplantationDirectory.FullName, "registration"));
                if (preTransformationsDirectory != null && preTransformationsDirectory.Exists)
                {
                    preTransformation = preTransformationsDirectory.GetFiles("RawT1-" + parent.Name + "_" + preimplantationDirectory.Name + "_TO_Scanner_Based.trm").FirstOrDefault();
                }
            }
            string preTransformationPath = string.Empty;
            if (preTransformation != null && preTransformation.Exists) preTransformationPath = preTransformation.FullName;
            // Post
            DirectoryInfo postimplantationDirectory = null, postTransformationsDirectory = null;
            FileInfo postTransformation = null;
            postimplantationDirectory = t1mr1.GetDirectories("T1post_*").FirstOrDefault();
            if (postimplantationDirectory != null && postimplantationDirectory.Exists)
            {
                postTransformationsDirectory = new DirectoryInfo(Path.Combine(postimplantationDirectory.FullName, "registration"));
                if (postTransformationsDirectory != null && postTransformationsDirectory.Exists)
                {
                    postTransformation = postTransformationsDirectory.GetFiles("RawT1-" + parent.Name + "_" + postimplantationDirectory.Name + "_TO_Scanner_Based.trm").FirstOrDefault();
                }
            }
            string postTransformationPath = string.Empty;
            if (postTransformation != null && postTransformation.Exists) postTransformationPath = postTransformation.FullName;
            // Mesh
            DirectoryInfo meshDirectory = new DirectoryInfo(Path.Combine(preimplantationDirectory.FullName, "default_analysis", "segmentation", "mesh"));
            if(meshDirectory.Exists)
            {
                FileInfo greyMatterLeftHemisphere = new FileInfo(Path.Combine(meshDirectory.FullName, parent.Name + "_Lhemi" + MESH_EXTENSION));
                FileInfo greyMatterRightHemisphere = new FileInfo(Path.Combine(meshDirectory.FullName, parent.Name + "_Rhemi" + MESH_EXTENSION));
                if (greyMatterLeftHemisphere.Exists && greyMatterRightHemisphere.Exists)
                {
                    meshes.Add(new LeftRightMesh("Grey matter", preTransformationPath, greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName, string.Empty, string.Empty));
                    if (!string.IsNullOrEmpty(postTransformationPath))
                    {
                        meshes.Add(new LeftRightMesh("Grey matter post", postTransformationPath, greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName, string.Empty, string.Empty));
                    }
                }

                FileInfo whiteMatterLeftHemisphere = new FileInfo(Path.Combine(meshDirectory.FullName, parent.Name + "_Lwhite" + MESH_EXTENSION));
                FileInfo whiteMatterRightHemisphere = new FileInfo(Path.Combine(meshDirectory.FullName, parent.Name + "_Rwhite" + MESH_EXTENSION));
                DirectoryInfo SurfaceAnalysisDirectory = new DirectoryInfo(Path.Combine(meshDirectory.FullName, "surface_analysis"));
                FileInfo marsAtlasLeftHemisphere = new FileInfo(Path.Combine(SurfaceAnalysisDirectory.FullName, parent.Name + "_Lwhite_parcels_marsAtlas" + MESH_EXTENSION));
                FileInfo marsAtlasRightHemisphere = new FileInfo(Path.Combine(SurfaceAnalysisDirectory.FullName, parent.Name + "_Rwhite_parcels_marsAtlas" + MESH_EXTENSION));
                string marsAtlasLeftHemispherePath = marsAtlasLeftHemisphere.Exists ? marsAtlasLeftHemisphere.FullName : string.Empty;
                string marsAtlasRightHemispherePath = marsAtlasRightHemisphere.Exists ? marsAtlasRightHemisphere.FullName : string.Empty;
                if (whiteMatterLeftHemisphere.Exists && whiteMatterRightHemisphere.Exists)
                {
                    meshes.Add(new LeftRightMesh("White matter", preTransformationPath, whiteMatterLeftHemisphere.FullName, whiteMatterRightHemisphere.FullName, marsAtlasLeftHemispherePath, marsAtlasRightHemispherePath));
                    if (!string.IsNullOrEmpty(postTransformationPath))
                    {
                        meshes.Add(new LeftRightMesh("White matter post", postTransformationPath, whiteMatterLeftHemisphere.FullName, whiteMatterRightHemisphere.FullName, marsAtlasLeftHemispherePath, marsAtlasRightHemispherePath));
                    }
                }
            }
            return meshes.ToArray();
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new BaseMesh(Name, Transformation, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is BaseMesh mesh)
            {
                Name = mesh.Name;
                Transformation = mesh.Transformation;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            SavedTransformation = SavedTransformation.StandardizeToEnvironement();
            RecalculateUsable();
            base.OnDeserialized();
        }
        #endregion
    }
}