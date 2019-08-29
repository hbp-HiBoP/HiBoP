using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    /// <summary>
    /// Contains all the data about a mesh.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the mesh.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Mesh : BaseData
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

        public bool WasUsable { get; protected set; }
        public bool Usable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasMesh;
                WasUsable = usable;
                return usable;
            }
        }
        public virtual bool HasMesh { get { return false; } }
        public virtual bool HasMarsAtlas { get { return false; } }
        public virtual bool HasTransformation
        {
            get
            {
                return !string.IsNullOrEmpty(Transformation) && File.Exists(Transformation) && (new FileInfo(Transformation).Extension == TRANSFORMATION_EXTENSION || new FileInfo(Transformation).Extension == ".txt");
            }
        }
        [DataMember(Order = 5, Name = "Transformation")] protected string m_Transformation;
        public string Transformation
        {
            get
            {
                return m_Transformation.ConvertToFullPath();
            }
            set
            {
                m_Transformation = value.ConvertToShortPath();
            }
        }
        public string SavedTransformation { get { return m_Transformation; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Anatomy.Mesh">Mesh</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        /// <param name="id">Unique identifier to identify the group.</param>
        public Mesh(string name, string transformation, string id): base(id)
        {
            Name = name;
            Transformation = transformation;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="HBP.Data.Group">Group</see> class.
        /// </summary>
        /// <param name="name">Name of the group.</param>
        /// <param name="patients">Patients of the group.</param>
        /// <param name="id">Unique identifier to identify the group.</param>
        public Mesh(string name, string transformation) : base()
        {
            Name = name;
            Transformation = transformation;
        }
        public Mesh() : this("New mesh", string.Empty)
        {
        }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        public static Mesh[] GetMeshesInDirectory(string path)
        {
            List<Mesh> meshes = new List<Mesh>();
            DirectoryInfo parent = new DirectoryInfo(path);
            DirectoryInfo t1mr1 = new DirectoryInfo(Path.Combine(path, "t1mri"));

            DirectoryInfo preimplantationDirectory = null, preTransformationsDirectory = null;
            FileInfo preTransformation = null;
            preimplantationDirectory = t1mr1.GetDirectories("T1pre_*").FirstOrDefault();
            if (preimplantationDirectory != null)
            {
                preTransformationsDirectory = new DirectoryInfo(Path.Combine(preimplantationDirectory.FullName, "registration"));
                if (preTransformationsDirectory != null)
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
            if (postimplantationDirectory != null)
            {
                postTransformationsDirectory = new DirectoryInfo(Path.Combine(postimplantationDirectory.FullName, "registration"));
                if (postTransformationsDirectory != null)
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
            return new Mesh(Name, Transformation, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is Mesh mesh)
            {
                Name = mesh.Name;
                Transformation = mesh.Transformation;
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            OnDeserializedOperation(context);
        }
        protected virtual void OnDeserializedOperation(StreamingContext context)
        {
            m_Transformation = m_Transformation.ToPath();
            RecalculateUsable();
        }
        #endregion
    }
}