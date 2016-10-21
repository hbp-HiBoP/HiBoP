using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Experience.Dataset;


namespace HBP.Data.Visualisation
{
    [Serializable]
    public class MultiPatientsVisualisation : Visualisation
    {
        #region Properties
        [SerializeField]
        private List<string> patientsID;
        public ReadOnlyCollection<Patient.Patient> Patients
        {
            get { return new ReadOnlyCollection<Patient.Patient>((from patient in ApplicationState.ProjectLoaded.Patients where patientsID.Contains(patient.ID) select patient).ToList()); }
        }
        #endregion

        #region Constructors
        public MultiPatientsVisualisation(string name,List<Column> columns, List<Patient.Patient> patients, string id) : base(name,columns,id)
        {
            SetPatients(patients.ToArray());
        }
        public MultiPatientsVisualisation(string name,List<Column> columns, List<Patient.Patient> patients) : base (name,columns)
        {
            SetPatients(patients.ToArray());
        }
        public MultiPatientsVisualisation() : base()
        {
            ClearPatients();
        }
        #endregion

        #region Public Methods
        public void AddPatient(Patient.Patient patient)
        {
            patientsID.Add(patient.ID);
        }
        public void AddPatient(Patient.Patient[] patients)
        {
            foreach(Patient.Patient patient in patients)
            {
                AddPatient(patient);
            }
        }
        public void RemovePatient(Patient.Patient patient)
        {
            patientsID.Remove(patient.ID);
        }
        public void RemovePatient(Patient.Patient[] patients)
        {
            foreach (Patient.Patient patient in patients)
            {
                RemovePatient(patient);
            }
        }
        public void SetPatients(Patient.Patient[] patients)
        {
            patientsID = (from patient in patients select patient.ID).ToList();
        }
        public void ClearPatients()
        {
            patientsID = new List<string>();
        }
        public override DataInfo[] GetDataInfo(Column column)
        {
            List<DataInfo> result = new List<DataInfo>();
            foreach (DataInfo dataInfo in column.Dataset.Data)
            {
                if (column.DataLabel == dataInfo.Name  && Patients.Contains(dataInfo.Patient) && column.Protocol == dataInfo.Protocol && dataInfo.Protocol.Blocs.Contains(column.Bloc))
                {
                    result.Add(dataInfo);
                }
            }
            return result.ToArray();
        }
        public DataInfo GetDataInfo(Patient.Patient patient, Column column)
        {
            DataInfo l_dataInfo = new DataInfo();
            foreach (DataInfo dataInfo in column.Dataset.Data)
            {
                if (dataInfo.Name == column.DataLabel && dataInfo.Patient == patient && dataInfo.Protocol.Blocs.Contains(column.Bloc))
                {
                    l_dataInfo = dataInfo;
                }
            }
            return l_dataInfo;
        }
        public string[] GetImplantation()
        {
            return (from patient in Patients select patient.Brain.MNIBasedImplantation).ToArray();
        }
        public override void SaveXML(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MultiPatientsVisualisation));
            TextWriter textWriter = new StreamWriter(GenerateSavePath(path));
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }
        public override void SaveJSon(string path)
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(GenerateSavePath(path)))
            {
                outPutFile.Write(l_json);
            }
        }
        public static MultiPatientsVisualisation LoadXML(string path)
        {
            MultiPatientsVisualisation result = new MultiPatientsVisualisation();
            if (File.Exists(path) && Path.GetExtension(path) == Settings.FileExtension.MultiVisualisation)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MultiPatientsVisualisation));
                TextReader textReader = new StreamReader(path);
                result = serializer.Deserialize(textReader) as MultiPatientsVisualisation;
                textReader.Close();
            }
            return result;
        }
        public static MultiPatientsVisualisation LoadJSon(string path)
        {
            MultiPatientsVisualisation result = new MultiPatientsVisualisation();
            try
            {
                using (StreamReader inPutFile = new StreamReader(path))
                {
                    result = JsonUtility.FromJson<MultiPatientsVisualisation>(inPutFile.ReadToEnd());
                }
            }
            catch
            {
                Debug.LogWarning("Can't read the multi patients visualisation file.");
            }
            return result;
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + Settings.FileExtension.MultiVisualisation;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + Settings.FileExtension.MultiVisualisation);
            }
            return l_finalPath;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MultiPatientsVisualisation(Name, new List<Column>(Columns), new List<Patient.Patient>(Patients), ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            MultiPatientsVisualisation visu = copy as MultiPatientsVisualisation;
            SetPatients(visu.Patients.ToArray());
        }
        #endregion
    }
}