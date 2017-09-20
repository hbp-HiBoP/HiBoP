using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public abstract class Mesh : ICloneable, ICopiable
    {
        #region Properties
        public const string EXTENSION = ".gii";
        [DataMember(Order = 0)] public string Name { get; set; }
        [DataMember] public string ID { get; set; }
        public virtual bool Usable
        {
            get { return !string.IsNullOrEmpty(Name) && HasMesh; }
        }
        public abstract bool HasMesh { get; }
        public abstract bool HasMarsAtlas { get; }
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
        #endregion

        #region Public Methods
        public static Mesh[] GetMeshes(string path)
        {
            List<Mesh> meshes = new List<Mesh>();
            DirectoryInfo parent = new DirectoryInfo(path);
            DirectoryInfo t1mr1 = new DirectoryInfo(path + Path.DirectorySeparatorChar + "t1mri");
            DirectoryInfo preoperativeDirectory = new DirectoryInfo(t1mr1.GetDirectories("T1pre_*").First().FullName);
            DirectoryInfo transformationsDirectory = new DirectoryInfo(preoperativeDirectory.FullName + Path.DirectorySeparatorChar + "registration");
            FileInfo transformation = transformationsDirectory.GetFiles("RawT1-" + parent.Name + "_" + preoperativeDirectory.Name + "_TO_Scanner_Based.trm").FirstOrDefault();
            string transformationPath = string.Empty;
            if (transformation != null && transformation.Exists) transformationPath = transformation.FullName;
            DirectoryInfo meshDirectory = new DirectoryInfo(preoperativeDirectory.FullName + Path.DirectorySeparatorChar + "default_analysis" + Path.DirectorySeparatorChar + "segmentation" + Path.DirectorySeparatorChar + "mesh");
            if(meshDirectory.Exists)
            {
                FileInfo greyMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lhemi" + EXTENSION);
                FileInfo greyMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rhemi" + EXTENSION);
                if(greyMatterLeftHemisphere.Exists && greyMatterRightHemisphere.Exists) meshes.Add(new LeftRightMesh("Grey matter", transformationPath, greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName,string.Empty,string.Empty));

                FileInfo whiteMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lwhite" + EXTENSION);
                FileInfo whiteMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rwhite" + EXTENSION);
                DirectoryInfo SurfaceAnalysisDirectory = new DirectoryInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + "surface_analysis");
                FileInfo marsAtlasLeftHemisphere = new FileInfo(SurfaceAnalysisDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lwhite_parcels_marsAtlas" + EXTENSION);
                FileInfo marsAtlasRightHemisphere = new FileInfo(SurfaceAnalysisDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rwhite_parcels_marsAtlas" + EXTENSION);
                string marsAtlasLeftHemispherePath = marsAtlasLeftHemisphere.Exists ? marsAtlasLeftHemisphere.FullName : string.Empty;
                string marsAtlasRightHemispherePath = marsAtlasRightHemisphere.Exists ? marsAtlasRightHemisphere.FullName : string.Empty;
                if (whiteMatterLeftHemisphere.Exists && whiteMatterRightHemisphere.Exists) meshes.Add(new LeftRightMesh("White matter", transformationPath, whiteMatterLeftHemisphere.FullName, whiteMatterRightHemisphere.FullName, marsAtlasLeftHemispherePath, marsAtlasRightHemispherePath));
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
        public abstract object Clone();
        public virtual void Copy(object copy)
        {
            Mesh mesh = copy as Mesh;
            Name = mesh.Name;
            Transformation = mesh.Transformation;
            ID = mesh.ID;
        }
        #endregion
    }
}