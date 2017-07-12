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
        public virtual bool isUsable
        {
            get { return !string.IsNullOrEmpty(Name); }
        }
        #endregion

        #region Constructor
        public Mesh(string name)
        {
            Name = name;
        }
        #endregion

        #region Public Methods
        public static Mesh[] GetMeshes(string path)
        {
            List<Mesh> meshes = new List<Mesh>();
            DirectoryInfo parent = new DirectoryInfo(path);
            DirectoryInfo t1mr1 = new DirectoryInfo(path + Path.DirectorySeparatorChar + "t1mri");
            DirectoryInfo meshDirectory = new DirectoryInfo(t1mr1.GetDirectories("T1pre_*").First().FullName + Path.DirectorySeparatorChar + "default_analysis" + Path.DirectorySeparatorChar + "segmentation" + Path.DirectorySeparatorChar + "mesh");
            if(meshDirectory.Exists)
            {
                FileInfo greyMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lhemi" + EXTENSION);
                FileInfo greyMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rhemi" + EXTENSION);
                if(greyMatterLeftHemisphere.Exists && greyMatterRightHemisphere.Exists) meshes.Add(new LeftRightMesh("Grey matter", greyMatterLeftHemisphere.FullName, greyMatterRightHemisphere.FullName));

                FileInfo whiteMatterLeftHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Lwhite" + EXTENSION);
                FileInfo whiteMatterRightHemisphere = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + parent.Name + "_Rwhite" + EXTENSION);
                if (whiteMatterLeftHemisphere.Exists && whiteMatterRightHemisphere.Exists) meshes.Add(new LeftRightMesh("White matter", whiteMatterLeftHemisphere.FullName, whiteMatterRightHemisphere.FullName));
            }
            return meshes.ToArray();
        }
        #endregion

        #region Operators
        public virtual object Clone()
        {
            return new Mesh(Name);
        }
        public virtual void Copy(object copy)
        {
            Name = (copy as Mesh).Name;
        }
        #endregion
    }
}