using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;


namespace HBP.Data.Visualisation
{
    [Serializable]
    public class SinglePatientVisualisation : Visualisation
    {
        #region Properties
        [SerializeField]
        private string patientID = string.Empty;
        public Patient.Patient Patient
        {
            get
            {
                Patient.Patient tmp = ApplicationState.ProjectLoaded.Patients.FirstOrDefault(p => p.ID == patientID);
                if (tmp == null) tmp = new Patient.Patient();
                return tmp;
            }
            set { patientID = value.ID; }
        }
        #endregion

        #region Constructors
        public SinglePatientVisualisation(string name,List<Column> columns, Patient.Patient patient, string id) : base(name,columns,id)
        {
            Patient = patient;
        }
        public SinglePatientVisualisation(string name,List<Column> columns,Patient.Patient patient) : base (name,columns)
        {
            Patient = patient;
        }
        public SinglePatientVisualisation() : base()
        {
            Patient = new Patient.Patient();
        }
        #endregion

        #region Public Methods
        public override DataInfo[] GetDataInfo(Column column)
        {
            List<DataInfo> result = new List<DataInfo>();
            foreach (DataInfo dataInfo in column.Dataset.Data)
            {
                if (column.DataLabel == dataInfo.Name  && Patient == dataInfo.Patient && column.Protocol == dataInfo.Protocol && dataInfo.Protocol.Blocs.Contains(column.Bloc))
                {
                    result.Add(dataInfo);
                }
            }
            return result.ToArray();
        }
        public string GetImplantation()
        {
            return Patient.Brain.PatientBasedImplantation;
        }
        public override void SaveXML(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SinglePatientVisualisation));
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
        public static SinglePatientVisualisation LoadXML(string path)
        {
            SinglePatientVisualisation result = new SinglePatientVisualisation();
            if (File.Exists(path) && Path.GetExtension(path) == Settings.FileExtension.SingleVisualisation)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SinglePatientVisualisation));
                TextReader textReader = new StreamReader(path);
                result = serializer.Deserialize(textReader) as SinglePatientVisualisation;
                textReader.Close();
            }
            return result;
        }
        public static SinglePatientVisualisation LoadJSon(string path)
        {
            SinglePatientVisualisation result = new SinglePatientVisualisation();
            try
            {
                using (StreamReader inPutFile = new StreamReader(path))
                {
                    result = JsonUtility.FromJson<SinglePatientVisualisation>(inPutFile.ReadToEnd());
                }
            }
            catch
            {
                Debug.LogWarning("Can't read the single patient visualisation file.");
            }
            return result;
        }
        public static SinglePatientVisualisation LoadFromMultiPatients(MultiPatientsVisualisation multiPatientsVisualisation,int patient)
        {
            return new SinglePatientVisualisation(multiPatientsVisualisation.Name, multiPatientsVisualisation.Columns.ToList(), multiPatientsVisualisation.Patients[patient]);
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + Settings.FileExtension.SingleVisualisation;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + Settings.FileExtension.SingleVisualisation);
            }
            return l_finalPath;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SinglePatientVisualisation(Name, new List<Column>(Columns), Patient);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            Patient = (copy as SinglePatientVisualisation).Patient;
        }
        #endregion
    }
}