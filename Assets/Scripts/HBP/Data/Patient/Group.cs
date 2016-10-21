using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Settings;

namespace HBP.Data.Patient
{
    /// <summary>
    /// Class which define a group of patients in the project.
    /// </summary>
    [Serializable]
    public class Group : ICloneable , ICopiable
    {
        #region Properties
        string id;
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
            set { name=value; }
        }

        [SerializeField]
        private List<string> patientsID;
        private List<Patient> patients
        {
            get { return (from patient in ApplicationState.ProjectLoaded.Patients where patientsID.Contains(patient.ID) select patient).ToList(); }
            set { patientsID = (from patient in value select patient.ID).ToList(); }
        }
        public ReadOnlyCollection<Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient>((from patient in ApplicationState.ProjectLoaded.Patients where patientsID.Contains(patient.ID) select patient).ToList()); }
        }
        #endregion

        #region Constructors
        public Group(string name, Patient[] patientsInTheGroup,string id)
        {
            Name = name;
            patients = patientsInTheGroup.ToList();
            ID = id;
        }
        public Group(string name, Patient[] patientsInTheGroup): this(name,patientsInTheGroup,Guid.NewGuid().ToString())
        {
        }
        public Group() : this("New Group",new Patient[0])
		{
		}
		#endregion

		#region Public Methods
        public void Add(Patient patient)
        {
            if(!patientsID.Contains(patient.ID))
            {
                patientsID.Add(patient.ID);
            }
        }
        public void Add(Patient[] patients)
        {
            foreach (Patient patient in patients) Add(patient);
        }
        public void Remove(Patient patient)
        {
            patientsID.Remove(patient.ID);
        }
        public void Remove(Patient[] patients)
        {
            foreach (Patient patient in patients) Remove(patient);
        }
        public void SaveXML(string path)
        {
            XmlSerializer l_groupXmlSerializer = new XmlSerializer(typeof(Group));
            TextWriter l_groupSW = new StreamWriter(GenerateSavePath(path));
            l_groupXmlSerializer.Serialize(l_groupSW, this);
            l_groupSW.Close();
        }
        public void SaveJSon(string path)
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(GenerateSavePath(path)))
            {
                outPutFile.Write(l_json);
            }
        }
        public static Group LoadXML(string path)
        {
            if (File.Exists(path) && Path.GetExtension(path) == FileExtension.Group)
            {
                XmlSerializer l_groupsXmlSerializer = new XmlSerializer(typeof(Group));
                TextReader l_groupsSR = new StreamReader(path);
                Group l_group = l_groupsXmlSerializer.Deserialize(l_groupsSR) as Group;
                l_groupsSR.Close();
                return l_group;
            }
            else
            {
                return new Group();
            }
        }
        public static Group LoadJSon(string path)
        {
            string l_json = string.Empty;
            using (StreamReader inPutFile = new StreamReader(path))
            {
                return JsonUtility.FromJson<Group>(inPutFile.ReadToEnd());
            }
        }
		#endregion

        #region Operator
        public object Clone()
        {
            return new Group(Name, Patients.ToArray().Clone() as Patient[],ID);
        }
        public override bool Equals(object obj)
        {
            Group p = obj as Group;
            if (p == null)
            {
                return false;
            }
            else
            {
                return ID == p.ID;
            }
        }
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        public void Copy(object copy)
        {
            Group group = copy as Group;
            Name = group.Name;
            patients = group.Patients.ToList();
            ID = group.ID;
        }
        public static bool operator ==(Group a, Group b)
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
        public static bool operator !=(Group a, Group b)
        {
            return !(a == b);
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + FileExtension.Group;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + FileExtension.Group);
            }
            return l_finalPath;
        }
        #endregion
    }
}