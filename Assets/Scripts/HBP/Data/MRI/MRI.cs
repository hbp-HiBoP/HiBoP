using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Tools.Unity;

namespace HBP.Data
{
    /// <summary>
    /// A class which contains all the data about a Magnetic resonance imaging (MRI).
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
    /// <description>Name of the MRI.</description>
    /// </item>
    /// <item>
    /// <term><b>File</b></term>
    /// <description>MRI file</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class MRI : BaseData, INameable
    {
        #region Properties
        /// <summary>
        /// Extension of MRI files.
        /// </summary>
        public const string EXTENSION = ".nii";
        /// <summary>
        /// Name of the MRI.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// MRI file path with Alias.
        /// </summary>
        [DataMember(Name = "File")] public string SavedFile { get; protected set; }
        /// <summary>
        /// MRI file path without Alias.
        /// </summary>
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
        /// <summary>
        /// True if the MRI was usable, False otherwise.
        /// </summary>
        public bool WasUsable { get; protected set; }
        /// <summary>
        /// True if the MRI is usable, False otherwise.
        /// </summary>
        public bool IsUsable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasMRI;
                WasUsable = usable;
                return usable;
            }
        }
        /// <summary>
        /// True if the MRI has MRI file, False otherwise.
        /// </summary>
        public virtual bool HasMRI
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == EXTENSION);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new MRI instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="path">MRI file path</param>
        /// <param name="ID">Unique identifier</param>
        public MRI(string name, string path, string ID) : base(ID)
        {
            Name = name;
            File = path;
            RecalculateIsUsable();
        }
        /// <summary>
        /// Create a new MRI instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="path">MRI file path</param>
        public MRI(string name, string path) : base()
        {
            Name = name;
            File = path;
            RecalculateIsUsable();
        }
        /// <summary>
        /// Create a new MRI instance.
        /// </summary>
        public MRI() : this("New MRI", string.Empty) { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Recalculate if the MRI is usable.
        /// </summary>
        /// <returns></returns>
        public bool RecalculateIsUsable()
        {
            return IsUsable;
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Loads meshes from a directory.
        /// </summary>
        /// <param name="path">path of the directory.</param>
        /// <returns>All MRI in the directory</returns>
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
        protected override void OnDeserialized()
        {
            SavedFile = SavedFile.ToPath();
            RecalculateIsUsable();
            base.OnDeserialized();
        }
        #endregion
    }
}