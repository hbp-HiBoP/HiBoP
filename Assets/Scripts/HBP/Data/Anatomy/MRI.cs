using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class MRI : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        public const string EXTENSION = ".nii";
        [DataMember] public string ID { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember(Name = "File")] string m_File;
        public string File
        {
            get
            {
                return m_File.ConvertToFullPath();
            }
            set
            {
                m_File = value.ConvertToShortPath();
            }
        }
        public string SavedFile { get { return m_File; } }
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
                bool usable = !string.IsNullOrEmpty(Name) && HasMRI;
                m_WasUsable = usable;
                return usable;
            }
        }
        public virtual bool HasMRI
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == EXTENSION);
            }
        }
        #endregion

        #region Constructor
        public MRI(string name, string path, string id)
        {
            Name = name;
            File = path;
            ID = id;
            RecalculateUsable();
        }
        public MRI(string name, string path) : this(name, path, Guid.NewGuid().ToString())
        {

        }
        public MRI() : this("New MRI", string.Empty, Guid.NewGuid().ToString()) { }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        public static MRI[] GetMRIs(string path)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("GetMRIs");
            List<MRI> MRIs = new List<MRI>();
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path)) return MRIs.ToArray();
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectoryInfo t1mriDirectoy = directoryInfo.GetDirectories("t1mri", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if(t1mriDirectoy != null && t1mriDirectoy.Exists)
            {
                // Preimplantation.
                DirectoryInfo preimplantationDirectory = t1mriDirectoy.GetDirectories("T1pre_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (preimplantationDirectory != null && preimplantationDirectory.Exists)
                {
                    FileInfo preimplantationMRIFile = preimplantationDirectory.GetFiles(directoryInfo.Name + EXTENSION).FirstOrDefault();
                    if (preimplantationMRIFile != null && preimplantationMRIFile.Exists)
                    {
                        MRIs.Add(new MRI("Preimplantation", preimplantationMRIFile.FullName));
                    }
                }

                // Postimplantation.
                DirectoryInfo postimplantationDirectory = t1mriDirectoy.GetDirectories("T1post_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (postimplantationDirectory != null && postimplantationDirectory.Exists)
                {
                    FileInfo postimplantationMRIFile = postimplantationDirectory.GetFiles(directoryInfo.Name + EXTENSION).FirstOrDefault();
                    if (postimplantationMRIFile != null && postimplantationMRIFile.Exists)
                    {
                        MRIs.Add(new MRI("Postimplantation", postimplantationMRIFile.FullName));
                    }
                }
            }


            return MRIs.ToArray();
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
            MRI mri = obj as MRI;
            if (mri != null && mri.ID == ID)
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
        public static bool operator ==(MRI a, MRI b)
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
        public static bool operator !=(MRI a, MRI b)
        {
            return !(a == b);
        }
        public virtual void GenerateNewIDs()
        {
            ID = Guid.NewGuid().ToString();
        }
        public object Clone()
        {
            return new MRI(Name, File, ID);
        }
        public void Copy(object copy)
        {
            MRI mri = copy as MRI;
            Name = mri.Name;
            File = mri.File;
            ID = mri.ID;
            RecalculateUsable();
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            m_File = m_File.ToPath();
            RecalculateUsable();
        }
        #endregion
    }
}