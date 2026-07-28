using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
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
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class MRI : BaseData, INameable
    {
        #region Properties

        /// <summary>
        /// Extension of MRI files.
        /// </summary>
        public static readonly string[] EXTENSIONS = new string[] { ".nii", ".nii.gz", ".img" };

        /// <summary>
        /// Name of the MRI.
        /// </summary>
        [JsonProperty] public string Name { get; set; }

        /// <summary>
        /// MRI file path with Alias.
        /// </summary>
        [JsonProperty("File")] public string SavedFile { get; protected set; }

        /// <summary>
        /// MRI file path without Alias.
        /// </summary>
        public string File
        {
            get { return SavedFile.ConvertToFullPath(); }
            set { SavedFile = value.ConvertToShortPath(); }
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
            get { return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && EXTENSIONS.Any(e => e == new FileInfo(File).Extension); }
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
        public MRI() : this("New MRI", string.Empty)
        {
        }

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

        internal void ApplyUsabilityValidation(bool usable)
        {
            WasUsable = usable;
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
            List<MRI> MRIs = new();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return MRIs.ToArray();
            DirectoryInfo directoryInfo = new(path);
            DirectoryInfo t1mriDirectoy = directoryInfo.GetDirectories("t1mri", SearchOption.TopDirectoryOnly).FirstOrDefault();
            DirectoryInfo ct = directoryInfo.GetDirectories("ct", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (t1mriDirectoy != null && t1mriDirectoy.Exists)
            {
                // Pre-implantation.
                DirectoryInfo preimplantationDirectory = t1mriDirectoy.GetDirectories("T1pre_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (preimplantationDirectory != null && preimplantationDirectory.Exists)
                {
                    FileInfo preimplantationMRIFile = GetMRIFileWithExtensions(preimplantationDirectory, directoryInfo.Name);
                    if (preimplantationMRIFile != null && preimplantationMRIFile.Exists)
                    {
                        MRIs.Add(new MRI("Preimplantation", preimplantationMRIFile.FullName));
                    }
                }

                // Post-implantation.
                DirectoryInfo postimplantationDirectory = t1mriDirectoy.GetDirectories("T1post_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (postimplantationDirectory != null && postimplantationDirectory.Exists)
                {
                    FileInfo postimplantationMRIFile = GetMRIFileWithExtensions(postimplantationDirectory, directoryInfo.Name);
                    if (postimplantationMRIFile != null && postimplantationMRIFile.Exists)
                    {
                        MRIs.Add(new MRI("Postimplantation", postimplantationMRIFile.FullName));
                    }
                }

                // CT
                if (ct != null && ct.Exists)
                {
                    DirectoryInfo ctDirectory = ct.GetDirectories("CTpost_*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (ctDirectory != null && ctDirectory.Exists)
                    {
                        FileInfo ctMRIFile = GetMRIFileWithPattern(ctDirectory, directoryInfo.Name + "-CTPost_*");
                        if (ctMRIFile != null && ctMRIFile.Exists)
                        {
                            MRIs.Add(new MRI("CT", ctMRIFile.FullName));
                        }
                    }
                }
            }

            return MRIs.ToArray();
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Helper method to find MRI files with supported extensions.
        /// </summary>
        /// <param name="directory">Directory to search in</param>
        /// <param name="baseName">Base name of the file</param>
        /// <returns>First matching MRI file or null</returns>
        private static FileInfo GetMRIFileWithExtensions(DirectoryInfo directory, string baseName)
        {
            foreach (string extension in EXTENSIONS)
            {
                string fileName = baseName + extension;
                FileInfo file = new(Path.Combine(directory.FullName, fileName));
                if (file.Exists)
                {
                    return file;
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to find MRI files with a pattern and supported extensions.
        /// </summary>
        /// <param name="directory">Directory to search in</param>
        /// <param name="pattern">Pattern without extension</param>
        /// <returns>First matching MRI file or null</returns>
        private static FileInfo GetMRIFileWithPattern(DirectoryInfo directory, string pattern)
        {
            foreach (string extension in EXTENSIONS)
            {
                string searchPattern = pattern + extension;
                FileInfo[] files = directory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }

            return null;
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
            if (obj is MRI mri)
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
            SavedFile = SavedFile.StandardizeToEnvironement();
            base.OnDeserialized();
        }

        #endregion
    }
}
