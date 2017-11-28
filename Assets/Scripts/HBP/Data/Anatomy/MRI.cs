using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class MRI : ICloneable, ICopiable
    {
        #region Properties
        public const string EXTENSION = ".nii";
        [DataMember] public string Name { get; set; }
        [DataMember] public string File { get; set; }
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
        public MRI(string name, string path)
        {
            Name = name;
            File = path;
            RecalculateUsable();
        }
        public MRI() : this("New MRI", string.Empty) { }
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
                    MRIs.Add(new MRI("Posimplantation", postimplantationMRIFile.FullName));
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();

            return MRIs.ToArray();
        }
        #endregion

        #region Operators
        public object Clone()
        {
            return new MRI(Name, File);
        }
        public void Copy(object copy)
        {
            MRI mri = copy as MRI;
            Name = mri.Name;
            File = mri.File;
            RecalculateUsable();
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