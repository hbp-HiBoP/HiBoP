using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;

namespace HBP.Data.Settings
{
    /// <summary>
    /// Class which define the settings of a project :
    ///     - Name of the project.
    ///     - Localisation of the project.
    ///     - Localisation of the patients database.
    ///     - Localisation of the localizer database.
    /// </summary>
    [Serializable]
	public class ProjectSettings
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
            get { return name; } set { name = value; }
        }

        [SerializeField]
        private string patientDatabase;
        public string PatientDatabase
        {
            get { return patientDatabase; }
            set { patientDatabase = value; }
        }

        [SerializeField]
        private string localizerDatabase;
        public string LocalizerDatabase
        {
            get { return localizerDatabase; }
            set { localizerDatabase = value; }
        }
        #endregion

        #region Constructors
        public ProjectSettings(string name, string patientDatabaseLocalisation, string localizerDataBaseLocalisation)
        {
            Name = name;
            PatientDatabase = patientDatabaseLocalisation;
            LocalizerDatabase = localizerDataBaseLocalisation;
            ID = Guid.NewGuid().ToString();
        }
        public ProjectSettings(string name) : this(name, ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation, ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation)
        {
        }
        public ProjectSettings() : this(ApplicationState.GeneralSettings.DefaultProjectName, ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation, ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation)
        {
        }
        #endregion

        #region Public Methods
        public void SaveXML(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));
            TextWriter textWriter = new StreamWriter(GenerateSavePath(path));
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }
        public void SaveJSon(string path)
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(GenerateSavePath(path)))
            {
                outPutFile.Write(l_json);
            }
        }
        public static ProjectSettings LoadXML(string path)
        {
            ProjectSettings result = new ProjectSettings();
            if (File.Exists(path) && Path.GetExtension(path) == FileExtension.Settings)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectSettings));
                TextReader textReader = new StreamReader(path);
                result = serializer.Deserialize(textReader) as ProjectSettings;
                textReader.Close();
            }
            return result;
        }
        public static ProjectSettings LoadJson(string path)
        {
            ProjectSettings result = new ProjectSettings();
            try
            {
                using (StreamReader inPutFile = new StreamReader(path))
                {
                    result = JsonUtility.FromJson<ProjectSettings>(inPutFile.ReadToEnd());
                }
            }
            catch
            {
                Debug.LogWarning("Can't load the project settings file.");
            }
            return result;
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            string l_path = path + Path.DirectorySeparatorChar + Name;
            string l_finalPath = l_path + FileExtension.Settings;
            int count = 1;
            while (File.Exists(l_finalPath))
            {
                string tempFileName = string.Format("{0}({1})", l_path, count++);
                l_finalPath = Path.Combine(path, tempFileName + FileExtension.Dataset);
            }
            return l_finalPath;
        }
        #endregion
    }
}
