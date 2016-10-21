using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define a patient in the data base:
    ///     - Name : name of the patient.
    ///     - Date of studies : date on which the study was made.
    ///     - Place of studies : place on which the study was made.
    ///     - ID : unique identification informations.
    ///     - Brain : informations of the brain.(mesh,irm,implantation,...)
    ///     - Epilepsy type : epilepsy type of the patient.(IGE,IPE,SGE,SPE,Unknown)
    /// </summary>
    [Serializable]
	public class Patient : ICloneable , ICopiable
	{
        #region Properties
        [SerializeField]
        private string id;
        public string ID
        {
            get { return id; }
            private set { id = value; }
        }

        [SerializeField]
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [SerializeField]
        private int date;
        public int Date
        {
            get { return date; }
            set { date = value; }
        }

        [SerializeField]
        private string place;
        public string Place
        {
            get { return place; }
            set { place = value; }
        }

        [SerializeField]
        private Brain brain;
        public Brain Brain
        {
            get { return brain; }
            set { brain = value; }
        }
        #endregion

        #region Constructors
        public Patient(string name,string place,int date,Brain brain, string id)
        {
            Name = name;
            Place = place;
            Date = date;
            Brain = brain;
            ID = id;
        }
        public Patient(string name, string place, int date, Brain brain) : this(name,place,date,brain,Guid.NewGuid().ToString())
        {
        }
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
                            Brain.PreIRM = niiFile.FullName;
                        }
                        DirectoryInfo meshDirectory = new DirectoryInfo(preDirectory.FullName + Path.DirectorySeparatorChar + "default_analysis" + Path.DirectorySeparatorChar + "segmentation" + Path.DirectorySeparatorChar + "mesh");
                        if(meshDirectory.Exists)
                        {
                            FileInfo leftHemiFile = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + "_Lhemi.gii");
                            if(leftHemiFile.Exists)
                            {
                                Brain.LeftMesh = leftHemiFile.FullName;
                            }
                            FileInfo rightHemiFile = new FileInfo(meshDirectory.FullName + Path.DirectorySeparatorChar + directory.Name + "_Rhemi.gii");
                            if (rightHemiFile.Exists)
                            {
                                Brain.RightMesh = rightHemiFile.FullName;
                            }
                        }
                        DirectoryInfo registrationDirectory = new DirectoryInfo(preDirectory.FullName + Path.DirectorySeparatorChar + "registration");
                        if(registrationDirectory.Exists)
                        {
                            FileInfo transformFile = new FileInfo(registrationDirectory.FullName + Path.DirectorySeparatorChar + "RawT1-" + directory.Name + "_" + preDirectory.Name + "_TO_Scanner_Based.trm");
                            if(transformFile.Exists)
                            {
                                Brain.PreToScannerBasedTransformation = transformFile.FullName;
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
                            Brain.PostIRM = niiFile.FullName;
                        }
                    }
                }
            }
		}
        public Patient() : this("Unknown", "Unknown", 0, new Brain())
        {
        }
        #endregion

        #region Operators
        public override bool Equals(object obj)
        {
            Patient l_patient = obj as Patient;
            if (l_patient != null && l_patient.ID == ID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        public override string ToString()
        {
            return ID;
        }
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
        public static bool operator !=(Patient a, Patient b)
        {
            return !(a == b);
        }
        public object Clone()
        {
            return new Patient(Name, Place, Date, Brain.Clone() as Brain, ID);
        }
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
        public static string[] PatientsInDirectory(string path)
        {
            List<string> patients = new List<string>();
            if (Directory.Exists(path))
            {
                DirectoryInfo[] directories = new DirectoryInfo(path).GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    if (directory.Name.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries).Length == 3)
                    {
                        patients.Add(directory.FullName);
                    }
                }
            }
            return patients.ToArray();
        }
        public void SaveXML(string path)
        {
            XmlSerializer l_patientXmlSerializer = new XmlSerializer(typeof(Patient));
            TextWriter l_patientSW = new StreamWriter(GenerateSavePath(path));
            l_patientXmlSerializer.Serialize(l_patientSW, this);
            l_patientSW.Close();
        }
        public void SaveJSon(string path)
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(GenerateSavePath(path)))
            {
                outPutFile.Write(l_json);
            }
        }
        public static Patient LoadXML(string path)
        {
            Patient l_patient = new Patient();
            if (File.Exists(path) && Path.GetExtension(path) == Settings.FileExtension.Patient)
            {
                XmlSerializer l_patientsXmlSerializer = new XmlSerializer(typeof(Patient));
                TextReader l_patientsSR = new StreamReader(path);
                l_patient = l_patientsXmlSerializer.Deserialize(l_patientsSR) as Patient;
                l_patientsSR.Close();
            }
            return l_patient;
        }
        public static Patient LoadJSon(string path)
        {
            string l_json = string.Empty;
            using (StreamReader inPutFile = new StreamReader(path))
            {
                return JsonUtility.FromJson<Patient>(inPutFile.ReadToEnd());
            }
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Place + "_" + Date.ToString() + "_" + Name;
            string l_finalPath = l_path + Settings.FileExtension.Patient;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + Settings.FileExtension.Patient);
            }
            return l_finalPath;
        }
        #endregion
    }
}