using System;
using System.IO;
using System.Collections.Generic;
using HBP.Data.Anatomy;

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
    [Serializable]
	public class Patient : ICloneable, ICopiable
	{
        #region Properties
        public const string EXTENSION = ".patient";

        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Patient name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date of study.
        /// </summary>
        public int Date { get; set; }

        /// <summary>
        /// Place of study.
        /// </summary>
        public string Place { get; set; }

        /// <summary>
        /// Patient brain.
        /// </summary>
        public Brain Brain { get; set; }
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
        public Patient(string name,string place,int date,Brain brain, string id)
        {
            Name = name;
            Place = place;
            Date = date;
            Brain = brain;
            Brain.Patient = this;
            ID = id;
        }
        /// <summary>
        /// Create a new patient instance.
        /// </summary>
        /// <param name="name">Name of the patient.</param>
        /// <param name="place">Place of the study.</param>
        /// <param name="date">Date of the study.</param>
        /// <param name="brain">Brain of the patient.</param>
        public Patient(string name, string place, int date, Brain brain) : this(name,place,date,brain,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new patient instance.
        /// </summary>
        /// <param name="path">Directory path which contains the patient informations.</param>
		public Patient(string path) : this()
		{
            DirectoryInfo directory = new DirectoryInfo(path);
            string[] directoryNameParts = directory.Name.Split(new char[1] { '_' },StringSplitOptions.RemoveEmptyEntries);
			if(directoryNameParts.Length == 3)
			{
                int dateParsed; int.TryParse(directoryNameParts[1],out dateParsed) ;

                Place = directoryNameParts[0];
                Date = dateParsed;
                Name = directoryNameParts[2];
                ID = directory.Name;

                DirectoryInfo implantationDirectory = new DirectoryInfo(directory.FullName + Path.DirectorySeparatorChar + "implantation");
                if(implantationDirectory.Exists)
                {
                    FileInfo implantationFile = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + ".pts");
                    if(implantationFile.Exists)
                    {
                        Brain.PatientBasedImplantation = implantationFile.FullName;
                    }
                    FileInfo MNIimplantationFile = new FileInfo(implantationDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + "_MNI.pts");
                    if (MNIimplantationFile.Exists)
                    {
                        Brain.MNIBasedImplantation = MNIimplantationFile.FullName;
                    }
                }

                DirectoryInfo t1mriDirectoy = new DirectoryInfo(path + Path.DirectorySeparatorChar + "t1mri");
                if(t1mriDirectoy.Exists)
                {
                    DirectoryInfo[] preDirectories = t1mriDirectoy.GetDirectories("T1pre_*", SearchOption.TopDirectoryOnly);
                    if(preDirectories.Length > 0)
                    {
                        DirectoryInfo preDirectory = preDirectories[0];
                        FileInfo niiFile = new FileInfo(preDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + ".nii");
                        if(niiFile.Exists)
                        {
                            Brain.PreoperativeMRI = niiFile.FullName;
                        }
                        DirectoryInfo meshDirectory = new DirectoryInfo(preDirectory.FullName + Path.DirectorySeparatorChar + "default_analysis" + Path.DirectorySeparatorChar + "segmentation" + Path.DirectorySeparatorChar + "mesh");
                        if(meshDirectory.Exists)
                        {
                            FileInfo leftHemiFile = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + "_Lhemi.gii");
                            if(leftHemiFile.Exists)
                            {
                                Brain.LeftCerebralHemisphereMesh = leftHemiFile.FullName;
                            }
                            FileInfo rightHemiFile = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + "_Rhemi.gii");
                            if (rightHemiFile.Exists)
                            {
                                Brain.RightCerebralHemisphereMesh = rightHemiFile.FullName;
                            }
                        }
                        DirectoryInfo registrationDirectory = new DirectoryInfo(preDirectory.FullName + Path.DirectorySeparatorChar + "registration");
                        if(registrationDirectory.Exists)
                        {
                            FileInfo transformFile = new FileInfo(registrationDirectory.FullName + Path.DirectorySeparatorChar + "RawT1-" + directory.Name + "_" + preDirectory.Name + "_TO_Scanner_Based.trm");
                            if(transformFile.Exists)
                            {
                                Brain.PreoperativeBasedToScannerBasedTransformation = transformFile.FullName;
                            }
                        }
                    }
                    DirectoryInfo[] postDirectories = t1mriDirectoy.GetDirectories("T1post_*", SearchOption.TopDirectoryOnly);
                    if(postDirectories.Length > 0)
                    {
                        DirectoryInfo postDirectory = postDirectories[0];
                        FileInfo niiFile = new FileInfo(postDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + ".nii");
                        if(niiFile.Exists)
                        {
                            Brain.PostoperativeMRI = niiFile.FullName;
                        }
                    }
                }
            }
		}
        /// <summary>
        /// Create a new patient instance with default values.
        /// </summary>
        public Patient() : this("Unknown", "Unknown", 0, new Brain())
        {
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
            return new Patient(Name, Place, Date, Brain.Clone() as Brain, ID);
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
            ID = patient.ID;
            Brain = patient.Brain;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get patient directories in the directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns></returns>
        public static string[] GetPatientsDirectories(string path)
        {
            List<string> patientDirectories = new List<string>();
            if (Directory.Exists(path))
            {
                DirectoryInfo[] directories = new DirectoryInfo(path).GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    if (directory.Name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries).Length == 3)
                    {
                        patientDirectories.Add(directory.FullName);
                    }
                }
            }
            return patientDirectories.ToArray();
        }
        #endregion
    }
}