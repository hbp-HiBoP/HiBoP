using HBP.Data.Anatomy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data
{
    /// <summary>
    /// Contains all the data about a patient.
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
    /// <item>
    /// <term><b>Place</b></term>
    /// <description>Place where the patient had the operation.</description>
    /// </item>
    /// <item>
    /// <term><b>Meshes</b></term>
    /// <description>Meshes of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>MRIs</b></term>
    /// <description>MRI scans of the patient.</description>
    /// </item>
    /// <item>
    /// <term><b>Implantations</b></term>
    /// <description>Electrodes implantations of the patient.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Patient : BaseData, ILoadable<Patient>, ILoadableFromDatabase<Patient>
    {
        #region Properties
        /// <summary>
        /// Extension of patient files.
        /// </summary>
        public const string EXTENSION = ".patient";
        /// <summary>
        /// Name of the patient.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Year in which the patient was implanted.
        /// </summary>
        [DataMember] public int Date { get; set; }
        /// <summary>
        /// Place where the patient had the operation.
        /// </summary>
        [DataMember] public string Place { get; set; }
        /// <summary>
        /// Meshes of the patient.
        /// </summary>
        [DataMember] public List<Mesh> Meshes { get; set; }
        /// <summary>
        /// MRI scans of the patient.
        /// </summary>
        [DataMember] public List<MRI> MRIs { get; set; }
        /// <summary>
        /// Electrodes implantations of the patient.
        /// </summary>
        [DataMember] public List<Implantation> Implantations { get; set; }
        /// <summary>
        /// Complete name of the patient. Name (Place-Date).
        /// </summary>
        [IgnoreDataMember] public string CompleteName { get { return Name + " (" + Place + " - " + Date + ")"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Patient class.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="place">Place where the  patient had the operation.</param>
        /// <param name="date">Year in which the patient was implanted.</param>
        /// <param name="meshes">Meshes of the patient.</param>
        /// <param name="MRIs">MRI scans of the patient.</param>
        /// <param name="implantations">Electrodes implantations of the patient.</param>
        /// <param name="id">Unique identifier to identify the patient.</param>
        public Patient(string name, string place, int date, IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Implantation> implantations, string id) : base(id) 
        {
            Name = name;
            Place = place;
            Date = date;
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Implantations = implantations.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the Patient class.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="place">Place where the  patient had the operation.</param>
        /// <param name="date">Year in which the patient was implanted.</param>
        /// <param name="meshes">Meshes of the patient.</param>
        /// <param name="MRIs">MRI scans of the patient.</param>
        /// <param name="implantations">Electrodes implantations of the patient.</param>
        public Patient(string name, string place, int date, IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Implantation> implantations) : base()
        {
            Name = name;
            Place = place;
            Date = date;
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Implantations = implantations.ToList();
        }
        /// <summary>
        /// Initializes a new instance of the Patient class.
        /// </summary>
        public Patient() : this("Unknown", "Unknown", 0, new Mesh[0], new MRI[0], new Implantation[0])
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generates  ID recursively.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var mesh in Meshes) mesh.GenerateID();
            foreach (var mri in MRIs) mri.GenerateID();
            foreach (var implantation in Implantations) implantation.GenerateID();
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Gets the extension of the patient files.
        /// </summary>
        /// <returns></returns>
        public static string GetExtension()
        {
            return EXTENSION[0] == '.' ? EXTENSION.Substring(1) : EXTENSION;
        }
        /// <summary>
        /// Determines if the specified directory is a patient directory.
        /// </summary>
        /// <param name="path">The specified directory.</param>
        /// <returns><see langword="true"/> if the directory is a patient directory; otherwise, <see langword="false"/></returns>
        public static bool IsPatientDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return false;
            string[] nameElements = directory.Name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameElements.Length != 3) return false;
            DirectoryInfo[] directories = directory.GetDirectories();
            if (!directories.Any((dir) => dir.Name == "implantation") || !directories.Any((dir) => dir.Name == "t1mri")) return false;
            return true;
        }
        /// <summary>
        /// Loads patients from a directory
        /// </summary>
        /// <param name="path">The specified path of the patient directory.</param>
        /// <param name="result">The patient in the patient directory.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromDirectory(string path, out Patient result)
        {
            result = null;
            if (IsPatientDirectory(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                string[] directoryNameParts = directory.Name.Split(new char[1] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                int.TryParse(directoryNameParts[1], out int date);
                result = new Patient(directoryNameParts[2], directoryNameParts[0], date, Mesh.LoadFromDirectory(path), MRI.LoadFromDirectory(path), Implantation.LoadFromDirectory(path), directory.Name);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Loads patient from patient file.
        /// </summary>
        /// <param name="path">The specified path of the patient file.</param>
        /// <param name="result">The patient in the patient file.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromFile(string path, out Patient result)
        {
            result = null;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Patient>(path);
                return result != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(path));
            }
        }
        /// <summary>
        /// Loads patients from a database. 
        /// </summary>
        /// <param name="path">The specified path of the database.</param>
        /// <param name="result">The patients in the database.</param>
        /// <returns><see langword="true"/> if the method worked successfully; otherwise, <see langword="false"/></returns>
        public static bool LoadFromDatabase(string path, out Patient[] result)
        {
            result = null;
            if (string.IsNullOrEmpty(path)) return false;
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return false;
            IEnumerable<string> patientDirectories = (from dir in directory.GetDirectories() where IsPatientDirectory(dir.FullName) select dir.FullName).ToArray();
            List<Patient> patients = new List<Patient>(patientDirectories.Count());
            foreach (string dir in patientDirectories)
            {
                if (LoadFromDirectory(dir, out Patient patient))
                {
                    patients.Add(patient);
                }
            }
            result = patients.ToArray();
            return true;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Patient(Name, Place, Date, Meshes, MRIs, Implantations, ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Patient patient)
            {
                Name = patient.Name;
                Date = patient.Date;
                Place = patient.Place;
                Meshes = patient.Meshes;
                MRIs = patient.MRIs;
                Implantations = patient.Implantations;
            }
        }
        #endregion

        #region Interfaces
        string ILoadable<Patient>.GetExtension()
        {
            return GetExtension();
        }
        bool ILoadable<Patient>.LoadFromFile(string path, out Patient result)
        {
            return LoadFromFile(path, out result);
        }
        bool ILoadableFromDatabase<Patient>.LoadFromDatabase(string path, out Patient[] result)
        {
            return LoadFromDatabase(path, out result);
        }
        #endregion
    }
}