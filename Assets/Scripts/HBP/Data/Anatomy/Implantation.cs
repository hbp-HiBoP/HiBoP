using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Tools.Unity;

namespace HBP.Data.Anatomy
{
    /// <summary>
    /// Contains all the data about a electrodes implantation.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>Date</b></term>
    /// <description>Year in which the patient was implanted.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Implantation : BaseData
    {
        #region Properties
        /// <summary>
        /// Header of PTS files.
        /// </summary>
        const string PTS_HEADER = "ptsfile";
        /// <summary>
        /// Extension of PTS files.
        /// </summary>
        public const string PTS_EXTENSION = ".pts";
        /// <summary>
        /// Extension of BIDS files.
        /// </summary>
        public const string BIDS_EXTENSION = ".tsv";
        /// <summary>
        /// Extension of Mars atlas files.
        /// </summary>
        public const string MARS_ATLAS_EXTENSION = ".csv";     
        public enum Error { None, PathIsNullOrEmpty, FileNotFound, WrongExtension, CannotReadFile, WrongFormat, CannotReadAllSites };

        /// <summary>
        /// Name of the implantation.
        /// </summary>
        [DataMember] public string Name { get; set; }

        /// <summary>
        /// Path to the implantation file after aliases treatments.
        /// </summary>
        [DataMember(Name = "File")] public string SavedFile { get; private set; }
        /// <summary>
        /// Path to the implantation file.
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
        /// Path to the MarsAtlas file after aliases treatments.
        /// </summary>
        [DataMember(Name = "MarsAtlas")] public string SavedMarsAtlas { get; private set; }
        /// <summary>
        /// Path to the MarsAtlas file.
        /// </summary>
        public string MarsAtlas
        {
            get
            {
                return SavedMarsAtlas.ConvertToFullPath();
            }
            set
            {
                SavedMarsAtlas = value.ConvertToShortPath();
            }
        }

        [IgnoreDataMember] public List<Site> Sites { get; set; }
        [IgnoreDataMember] public Patient Patient { get; set; }
        public bool WasUsable { get; protected set; }
        public bool Usable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasImplantation;
                WasUsable = usable;
                return usable;
            }
        }

        public virtual bool HasImplantation
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == PTS_EXTENSION || new FileInfo(File).Extension == BIDS_EXTENSION);
            }
        }
        public virtual bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlas) && System.IO.File.Exists(MarsAtlas) && (new FileInfo(MarsAtlas).Extension == MARS_ATLAS_EXTENSION);
            }
        }
        #endregion

        #region Constructor
        public Implantation(string name, string path, string marsAtlas, string id)
        {
            Name = name;
            File = path;
            MarsAtlas = marsAtlas;
            Sites = new List<Site>();
            ID = id;
            RecalculateUsable();
        }
        public Implantation(string name, string path, string marsAtlas) : this(name, path, marsAtlas, Guid.NewGuid().ToString())
        {
        }
        public Implantation() : this("New implantation", string.Empty, string.Empty)
        {
        }
        #endregion

        #region Public Methods
        public bool RecalculateUsable()
        {
            return Usable;
        }
        public Error Load()
        {
            if (string.IsNullOrEmpty(File)) return Error.PathIsNullOrEmpty;
            FileInfo fileInfo = new FileInfo(File);
            if (!fileInfo.Exists) return Error.FileNotFound;
            if (fileInfo.Extension != PTS_EXTENSION) return Error.WrongExtension;
            string[] lines = new string[0];
            try
            {
                lines = System.IO.File.ReadAllLines(fileInfo.FullName);
            }
            catch
            {
                return Error.CannotReadFile;
            }
            if (!IsCorrect(lines)) return Error.WrongFormat;
            IEnumerable<Site> sites;
            if (!ReadSites(lines, out sites)) return Error.CannotReadAllSites;
            Sites = sites.ToList();
            return Error.None;
        }
        public void Unload()
        {
            Sites = new List<Site>();
        }
        public static Implantation[] GetImplantationsInDirectory(string path)
        {
            List<Implantation> implantations = new List<Implantation>();
            DirectoryInfo patientDirectory = new DirectoryInfo(path);
            if(patientDirectory != null && patientDirectory.Exists)
            {
                DirectoryInfo implantationDirectory = new DirectoryInfo(Path.Combine(path, "implantation"));
                if (implantationDirectory != null && implantationDirectory.Exists)
                {
                    FileInfo marsAtlasFile = new FileInfo(Path.Combine(implantationDirectory.FullName, patientDirectory.Name + MARS_ATLAS_EXTENSION));
                    string marsAtlas = marsAtlasFile.Exists ? marsAtlasFile.FullName : string.Empty;

                    FileInfo patientImplantation = new FileInfo(Path.Combine(implantationDirectory.FullName, patientDirectory.Name + PTS_EXTENSION));
                    if (patientImplantation != null && patientImplantation.Exists) implantations.Add(new Implantation("Patient", patientImplantation.FullName, marsAtlas));

                    FileInfo MNIImplantation = new FileInfo(Path.Combine(implantationDirectory.FullName, patientDirectory.Name + "_MNI" + PTS_EXTENSION));
                    if (MNIImplantation != null && MNIImplantation.Exists) implantations.Add(new Implantation("MNI", MNIImplantation.FullName, marsAtlas));

                    FileInfo ACPCImplantation = new FileInfo(Path.Combine(implantationDirectory.FullName, patientDirectory.Name + "_ACPC" + PTS_EXTENSION));
                    if (ACPCImplantation != null && ACPCImplantation.Exists) implantations.Add(new Implantation("ACPC", ACPCImplantation.FullName, marsAtlas));

                    FileInfo postImplantation = implantationDirectory.GetFiles(patientDirectory.Name + "_T1Post*" + PTS_EXTENSION).FirstOrDefault();
                    if (postImplantation != null && postImplantation.Exists) implantations.Add(new Implantation("Post", postImplantation.FullName, marsAtlas));
                }
            }

            return implantations.ToArray();
        }
        #endregion

        #region Private Methods
        bool IsCorrect(string[] lines)
        {
            int sites = 0;
            return lines[0] == PTS_HEADER && int.TryParse(lines[2],out sites) && sites == (lines.Length - 3);
        }
        bool ReadSites(IEnumerable<string> lines, out IEnumerable<Site> sites)
        {
            sites = new List<Site>();
            bool ok = true;
            foreach (var line in lines)
            {
                Site site;
                if (Site.ReadLine(line,Patient, out site))
                {
                    ok &= true;
                    (sites as List<Site>).Add(site);
                }
                else
                {
                    ok = false;
                }
            }
            return ok;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Implantation(Name, File, MarsAtlas, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is Implantation implantation)
            {
                Name = implantation.Name;
                File = implantation.File;
                MarsAtlas = implantation.MarsAtlas;
                RecalculateUsable();
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            SavedFile = SavedFile.ToPath();
            m_MarsAtlas = m_MarsAtlas.ToPath();
            RecalculateUsable();
        }
        #endregion
    }
}