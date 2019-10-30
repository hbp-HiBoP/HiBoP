using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using Tools.Unity;
using UnityEngine;
using System;

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
        /// Extension of MarsAtlas files.
        /// </summary>
        public const string MARSATLAS_EXTENSION = ".csv";
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

        [IgnoreDataMember] public List<Site> Sites { get; set; } = new List<Site>();
        [IgnoreDataMember] public Patient Patient { get; set; }
        /// <summary>
        /// Specifies if a implantation was usable at the last verification. Don't perform the verification.
        /// </summary>
        public bool WasUsable { get; protected set; }
        /// <summary>
        /// Specifies if a implantation is usable.
        /// </summary>
        public bool IsUsable
        {
            get
            {
                bool usable = !string.IsNullOrEmpty(Name) && HasImplantation;
                WasUsable = usable;
                return usable;
            }
        }

        /// <summary>
        /// Specifies if a implantation has a implantation file.
        /// </summary>
        public virtual bool HasImplantation
        {
            get
            {
                return !string.IsNullOrEmpty(File) && System.IO.File.Exists(File) && (new FileInfo(File).Extension == PTS_EXTENSION || new FileInfo(File).Extension == BIDS_EXTENSION);
            }
        }
        /// <summary>
        /// Specifies if a implantation has a marsAtlas file.
        /// </summary>
        public virtual bool HasMarsAtlas
        {
            get
            {
                return !string.IsNullOrEmpty(MarsAtlas) && System.IO.File.Exists(MarsAtlas) && (new FileInfo(MarsAtlas).Extension == MARSATLAS_EXTENSION);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Implantation class.
        /// </summary>
        /// <param name="name">Name of the implantation.</param>
        /// <param name="path">Path of the implantation file.</param>
        /// <param name="marsAtlas">Path of the marsAtlas file.</param>
        /// <param name="ID">Unique identifier to identify the implantation.</param>
        public Implantation(string name, string path, string marsAtlas, string ID) : base(ID)
        {
            Name = name;
            File = path;
            MarsAtlas = marsAtlas;
            RecalculateIsUsable();
        }
        /// <summary>
        /// Initializes a new instance of the Implantation class.
        /// </summary>
        /// <param name="name">Name of the implantation.</param>
        /// <param name="path">Path of the implantation file.</param>
        /// <param name="marsAtlas">Path of the marsAtlas file.</param>
        public Implantation(string name, string path, string marsAtlas) : base()
        {
            Name = name;
            File = path;
            MarsAtlas = marsAtlas;
            RecalculateIsUsable();
        }
        /// <summary>
        /// Initializes a new instance of the Implantation class.
        /// </summary>
        public Implantation() : this("New implantation", string.Empty, string.Empty)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Recalculate if a implantation is usable.
        /// </summary>
        /// <returns></returns>
        public bool RecalculateIsUsable()
        {
            return IsUsable;
        }
        /// <summary>
        /// Load the implantation.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Unload the implantation.
        /// </summary>
        public void Unload()
        {
            Sites = new List<Site>();
        }
        /// <summary>
        ///  Loads implantations from a directory.
        /// </summary>
        /// <param name="path">path of the directory.</param>
        /// <param name="result">Implantations in the directory.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static Implantation[] LoadFromDirectory(string path)
        {
            List<Implantation> implantations = new List<Implantation>();
            DirectoryInfo patientDirectory = new DirectoryInfo(path);
            if (patientDirectory != null && patientDirectory.Exists)
            {
                DirectoryInfo implantationDirectory = new DirectoryInfo(Path.Combine(path, "implantation"));
                if (implantationDirectory != null && implantationDirectory.Exists)
                {
                    FileInfo marsAtlasFile = new FileInfo(Path.Combine(implantationDirectory.FullName, patientDirectory.Name + MARSATLAS_EXTENSION));
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
            return lines[0] == PTS_HEADER && int.TryParse(lines[2], out sites) && sites == (lines.Length - 3);
        }
        bool ReadSites(IEnumerable<string> lines, out IEnumerable<Site> sites)
        {
            sites = new List<Site>();
            bool ok = true;
            foreach (var line in lines)
            {
                Site site;
                if (Site.ReadLine(line, Patient, out site))
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
            if (obj is Implantation implantation)
            {
                Name = implantation.Name;
                File = implantation.File;
                MarsAtlas = implantation.MarsAtlas;
                RecalculateIsUsable();
            }
        }
        #endregion

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            SavedFile = SavedFile.ToPath();
            SavedMarsAtlas = SavedMarsAtlas.ToPath();
            RecalculateIsUsable();
        }
        #endregion

        /**
* \class Site
* \author Adrien Gannerie
* \version 1.0
* \date 26 avril 2017
* \brief Electrode site.
* 
* \details Class which define a electrode site  which contains:
*   - \a Name.
*   - \a Position.
*   - \a Reference to his parent electrode.
*/
        public class Site
        {
            #region Properties
            public string ID
            {
                get
                {
                    return Patient.ID + "_" + Name;
                }
            }
            public string Name { get; set; }
            public string Electrode
            {
                get
                {
                    return Name.Where((c) => !char.IsNumber(c)).ToString();
                }
            }
            public Patient Patient { get; set; }
            public Vector3 Position { get; set; }
            #endregion

            #region Constructors
            public Site(string name, Patient patient, Vector3 position)
            {
                Name = name;
                Patient = patient;
                Position = position;
            }
            public Site() : this("New site", null, Vector3.zero) { }
            #endregion

            #region Public static Methods
            public static bool ReadLine(string line, Patient patient, out Site site)
            {
                site = new Site();
                string[] elements = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split line into elements.
                float x, y, z;
                if (elements.Length >= 4 && Tools.CSharp.NumberExtension.TryParseFloat(elements[1], out x) && Tools.CSharp.NumberExtension.TryParseFloat(elements[2], out y) && Tools.CSharp.NumberExtension.TryParseFloat(elements[3], out z))
                {
                    site = new Site(elements[0], patient, new Vector3(x, y, z));
                    return true;
                }
                return false;
            }
            #endregion
        }
    }
}