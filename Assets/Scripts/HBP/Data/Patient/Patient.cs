using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Data.Anatomy;
using Tools.Unity;

namespace HBP.Data
{
    /**
    * \class Patient
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 janvier 2017
    * \brief Patient.
    * 
    * \details Class which contains:
    *     - \a Name : name of the patient.
    *     - \a Date \a of \a study : date on which the study was made.
    *     - \a Place \a of \a study : place on which the study was made.
    *     - \a ID : unique identification informations.
    *     - \a Brain : informations of the brain.(mesh,irm,implantation,...)
    *     - \a Epilepsy \a type : epilepsy type of the patient.(IGE,IPE,SGE,SPE,Unknown)
    */
    [DataContract]
	public class Patient : Object, ICloneable, ICopiable, ILoadable, IIdentifiable, ILoadableFromDatabase<Patient>
	{
        #region Properties
        /// <summary>
        /// Patient file extension.
        /// </summary>
        public static string EXTENSION = ".patient";
        /// <summary>
        /// Unique ID.
        /// </summary>
        [DataMember] public string ID { get; set; }
        /// <summary>
        /// Patient name.
        /// </summary>
        [DataMember]public string Name { get; set; }
        /// <summary>
        /// Date of study.
        /// </summary>
        [DataMember] public int Date { get; set; }
        /// <summary>
        /// Place of study.
        /// </summary>
        [DataMember] public string Place { get; set; }
        [DataMember] public List<Mesh> Meshes { get; set; }
        [DataMember] public List<MRI> MRIs { get; set; }
        [DataMember] public List<Connectivity> Connectivities { get; set; }
        [DataMember] public List<Implantation> Implantations { get; set; }
        [IgnoreDataMember] public string CompleteName { get { return Name + " (" + Place + " - " + Date + ")"; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new patient instance.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="place">Place of the study.</param>
        /// <param name="date">Date of the study.</param>
        /// <param name="brain">Brain of the patient.</param>
        /// <param name="id">Unique ID.</param>
        public Patient(string name,string place,int date, IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Connectivity> connectivities, IEnumerable<Implantation> implantations, string id)
        {
            Name = name;
            Place = place;
            Date = date;
            Meshes = meshes.ToList();
            this.MRIs = MRIs.ToList();
            Connectivities = connectivities.ToList();
            Implantations = implantations.ToList();
            ID = id;
        }
        /// <summary>
        /// Create a new patient instance.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="place">Place of the study.</param>
        /// <param name="date">Date of the study.</param>
        /// <param name="brain">Brain of the patient.</param>
        public Patient(string name, string place, int date, IEnumerable<Mesh> meshes, IEnumerable<MRI> MRIs, IEnumerable<Connectivity> connectivities, IEnumerable<Implantation> implantations) : this(name, place, date, meshes, MRIs, connectivities, implantations, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new patient instance.
        /// </summary>
        /// <param name="path">Directory path which contains the patient informations.</param>
		public Patient(string path) : this()
		{
            if(IsPatientDirectory(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                string[] directoryNameParts = directory.Name.Split(new char[1] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                int dateParsed; int.TryParse(directoryNameParts[1], out dateParsed);
                Place = directoryNameParts[0];
                Date = dateParsed;
                Name = directoryNameParts[2];
                ID = directory.Name;
                Meshes = Mesh.GetMeshes(path).ToList();
                MRIs = MRI.GetMRIs(path).ToList();
                Connectivities = new List<Connectivity>();
                Implantations = Implantation.GetImplantations(path).ToList();
            }
          
		}
        /// <summary>
        /// Create a new patient instance with default values.
        /// </summary>
        public Patient() : this("Unknown", "Unknown", 0, new Mesh[0], new MRI[0], new Connectivity[0], new Implantation[0])
        {
        }
        #endregion

        #region Public Methods
        public Patient[] LoadFromDatabase(string path)
        {
            return GetPatientsDirectories(path).Select(p => new Patient(p)).ToArray();
        }
        public void Load(string path)
        {
            Patient result;
            try
            {
                result = ClassLoaderSaver.LoadFromJson<Patient>(path);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw new CanNotReadPatientFileException(Path.GetFileNameWithoutExtension(path));
            }
            Copy(result);
        }
        public string GetExtension()
        {
            return EXTENSION[0] == '.'? EXTENSION.Substring(1) : EXTENSION;
        }
        /// <summary>
        /// Get patient directories in the directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns></returns>
        public static string[] GetPatientsDirectories(string path)
        {
            if (string.IsNullOrEmpty(path)) return new string[0];
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) return new string[0];
            return (from dir in directory.GetDirectories() where IsPatientDirectory(dir.FullName) select dir.FullName).ToArray();
        }
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
        #endregion

        #region Operators
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Patient patient = obj as Patient;
            if (patient != null && patient.ID == ID)
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
        /// Get string representation of a patient.
        /// </summary>
        /// <returns>Unique ID.</returns>
        public override string ToString()
        {
            return ID;
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First patient to compare.</param>
        /// <param name="b">Second patient to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Patient a, Patient b)
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
        /// <param name="a">First patient to compare.</param>
        /// <param name="b">Second patient to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Patient a, Patient b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Patient(Name, Place, Date, Meshes, MRIs, Connectivities, Implantations, ID);
        }
        public void GenerateNewIDs()
        {
            ID = Guid.NewGuid().ToString();
            foreach (var mesh in Meshes) mesh.GenerateNewIDs();
            foreach (var mri in MRIs) mri.GenerateNewIDs();
            foreach (var connectivity in Connectivities) connectivity.GenerateNewIDs();
            foreach (var implantation in Implantations) implantation.GenerateNewIDs();
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        public void Copy(object copy)
        {
            Patient patient = copy as Patient;
            Name = patient.Name;
            Date = patient.Date;
            Place = patient.Place;
            Meshes = patient.Meshes;
            MRIs = patient.MRIs;
            Connectivities = patient.Connectivities;
            Implantations = patient.Implantations;
            ID = patient.ID;
        }
        #endregion
    }
}