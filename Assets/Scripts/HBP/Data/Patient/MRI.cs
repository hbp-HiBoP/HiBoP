using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class MRI : BaseData
    {
        #region Properties
        public const string EXTENSION = ".nii";
        [DataMember] public string Name { get; set; }
        [DataMember(Name = "File")] public string SavedFile { get; protected set; }
        public string File
        {
            get
            {
                return SavedFile.ConvertToFullPath();
            }
            set
            {
                SavedFile = value.ConvertToShortPath();
            }
        }
        public bool WasUsable { get; protected set; }
        public bool IsUsable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasMRI;
                WasUsable = usable;
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

        #region Constructors
        public MRI(string name, string path, string ID) : base(ID)
        {
            Name = name;
            File = path;
            RecalculateIsUsable();
        }
        public MRI(string name, string path) : base()
        {
            Name = name;
            File = path;
            RecalculateIsUsable();
        }
        public MRI() : this("New MRI", string.Empty) { }
        #endregion

        #region Public Methods
        public bool RecalculateIsUsable()
        {
            return IsUsable;
        }
        #endregion

        #region Public Static Methods
        public static MRI[] LoadFromDirectory(string path)
        {
            List<MRI> MRIs = new List<MRI>();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return MRIs.ToArray();
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectoryInfo t1mriDirectoy = directoryInfo.GetDirectories("t1mri", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (t1mriDirectoy != null && t1mriDirectoy.Exists)
            {
                // Pre-implantation.
                DirectoryInfo preimplantationDirectory = t1mriDirectoy.GetDirectories("T1pre_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (preimplantationDirectory != null && preimplantationDirectory.Exists)
                {
                    FileInfo preimplantationMRIFile = preimplantationDirectory.GetFiles(directoryInfo.Name + EXTENSION).FirstOrDefault();
                    if (preimplantationMRIFile != null && preimplantationMRIFile.Exists)
                    {
                        MRIs.Add(new MRI("Preimplantation", preimplantationMRIFile.FullName));
                    }
                }

                // Post-implantation.
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
        public override object Clone()
        {
            return new MRI(Name, File, ID);
        }
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is MRI mri)
            {
                Name = mri.Name;
                File = mri.File;
                RecalculateIsUsable();
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            SavedFile = SavedFile.ToPath();
            RecalculateIsUsable();
        }
        #endregion
    }
}