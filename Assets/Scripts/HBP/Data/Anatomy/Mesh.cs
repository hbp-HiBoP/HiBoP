using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Mesh : ICloneable, ICopiable
    {
        #region Properties
        public const string EXTENSION = ".gii";
        [DataMember(Order = 0)] public string Name { get; set; }
        [DataMember] public string ID { get; set; }
        protected bool m_WasUsable;
        public bool WasUsable
        {
            get
            {
                return m_WasUsable;
            }
        }
        public bool Usable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasMesh;
                m_WasUsable = usable;
                return usable;
            }
        }
        public virtual bool HasMesh { get { return false; } }
        public virtual bool HasMarsAtlas { get { return false; } }
        public virtual bool HasTransformation
        {
            get
            {
                return !string.IsNullOrEmpty(Transformation) && File.Exists(Transformation) && (new FileInfo(Transformation).Extension == Anatomy.Transformation.EXTENSION || new FileInfo(Transformation).Extension == ".txt");
            }
        }
        [DataMember(Order = 5)] public string Transformation { get; set; }
        #endregion

        #region Constructor
        public Mesh(string name, string transformation, string ID)
        {
            Name = name;
            Transformation = transformation;
            this.ID = ID;
        }
        public Mesh(string name, string transformation) : this(name,transformation, Guid.NewGuid().ToString())
        {
        }
        public Mesh() : this("New mesh", string.Empty) { }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        public static Mesh[] GetMeshes(string path)
        {
            List<Mesh> meshes = new List<Mesh>();
            DirectoryInfo parent = new DirectoryInfo(path);
            DirectoryInfo t1mr1 = new DirectoryInfo(path + Path.DirectorySeparatorChar + "t1mri");
            // Pre
            DirectoryInfo preimplantationDirectory = null, preTransformationsDirectory = null;
            FileInfo preTransformation = null;
            preimplantationDirectory = t1mr1.GetDirectories("T1pre_*").FirstOrDefault();
            if (preimplantationDirectory != null)
            {
                preTransformationsDirectory = new DirectoryInfo(preimplantationDirectory.FullName + Path.DirectorySeparatorChar + "registration");
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
                postTransformationsDirectory = new DirectoryInfo(postimplantationDirectory.FullName + Path.DirectorySeparatorChar + "registration");
                if (postTransformationsDirectory != null)
                {
                    postTransformation = postTransformationsDirectory.GetFiles("RawT1-" + parent.Name + "_" + postimplantationDirectory.Name + "_TO_Scanner_Based.trm").FirstOrDefault();
                }
            }
            string postTransformationPath = string.Empty;
            if (postTransformation != null && postTransformation.Exists) postTransformationPath = postTransformation.FullName;
            // Mesh
            DirectoryInfo meshDirectory = new DirectoryInfo(preimplantationDirectory.FullName + Path.DirectorySeparatorChar + "default_analysis" + Path.DirectorySeparatorChar + "segmentation" + Path.DirectorySeparatorChar + "mesh");
            if(meshDirectory.Exists)
            {
                FileInfo greyMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lhemi" + EXTENSION);
                FileInfo greyMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rhemi" + EXTENSION);
                if (greyMatterLeftHemisphere.Exists && greyMatterRightHemisphere.Exists)
                {
                    meshes.Add(new LeftRightMesh("Grey matter", preTransformationPath, greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName, string.Empty, string.Empty));
                    if (!string.IsNullOrEmpty(postTransformationPath))
                    {
                        meshes.Add(new LeftRightMesh("Grey matter post", postTransformationPath, greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName, string.Empty, string.Empty));
                    }
                }

                FileInfo whiteMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lwhite" + EXTENSION);
                FileInfo whiteMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rwhite" + EXTENSION);
                DirectoryInfo SurfaceAnalysisDirectory = new DirectoryInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + "surface_analysis");
                FileInfo marsAtlasLeftHemisphere = new FileInfo(SurfaceAnalysisDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lwhite_parcels_marsAtlas" + EXTENSION);
                FileInfo marsAtlasRightHemisphere = new FileInfo(SurfaceAnalysisDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rwhite_parcels_marsAtlas" + EXTENSION);
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
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Mesh mesh = obj as Mesh;
            if (mesh != null && mesh.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Mesh a, Mesh b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">First mesh to compare.</param>
        /// <param name="b">Second mesh to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Mesh a, Mesh b)
        {
            return !(a == b);
        }
        public virtual object Clone()
        {
            return new Mesh(Name, Transformation, ID);
        }
        public virtual void Copy(object copy)
        {
            Mesh mesh = copy as Mesh;
            Name = mesh.Name;
            Transformation = mesh.Transformation;
            ID = mesh.ID;
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            RecalculateUsable();
        }
        #endregion
    }
}