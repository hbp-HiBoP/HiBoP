using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;

namespace HBP.Data.Settings
{
    /// <summary>
    /// Class which define the general settings of the application :
    ///     - default name of a project.
    ///     - default localisation of a project.
    ///     - default localisation of the patient database.
    ///     - default localisation of the localizer database.
    ///     - Option : Plot name automatic correction.
    /// </summary>
    [Serializable]
	public class GeneralSettings
	{
        #region Properties
        private static string PATH = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "GeneralSettings.txt";

        [SerializeField]
        private string defaultProjectName;
        public string DefaultProjectName
        {
            get { return defaultProjectName; }
            set { defaultProjectName = value; }
        }

        [SerializeField]
        private string defaultProjectLocation;
        public string DefaultProjectLocation
        {
            get { return defaultProjectLocation; }
            set { defaultProjectLocation = value; }
        }

        [SerializeField]
        private string defaultPatientDatabaseLocation;
        public string DefaultPatientDatabaseLocation
        {
            get { return defaultPatientDatabaseLocation; }
            set { defaultPatientDatabaseLocation = value; }
        }

        [SerializeField]
        private string defaultLocalizerDatabaseLocation;
        public string DefaultLocalizerDatabaseLocation
        {
            get { return defaultLocalizerDatabaseLocation; }
            set { defaultLocalizerDatabaseLocation = value; }
        }

        public enum PlotNameCorrectionTypeEnum { None, Active }
        [SerializeField]
        private PlotNameCorrectionTypeEnum plotNameAutomaticCorrectionType;
        public PlotNameCorrectionTypeEnum PlotNameAutomaticCorrectionType { get { return plotNameAutomaticCorrectionType; } set { plotNameAutomaticCorrectionType = value; } }

        public enum TrialMatrixSmoothingEnum { None, Line}
        [SerializeField]
        private TrialMatrixSmoothingEnum trialMatrixSmoothingType;
        public TrialMatrixSmoothingEnum TrialMatrixSmoothingType { get { return trialMatrixSmoothingType; } set { trialMatrixSmoothingType = value; } }

        public enum BaseLineTypeEnum { None, Line, Bloc, Protocol };
        [SerializeField]
        private BaseLineTypeEnum baseLineType;
        public BaseLineTypeEnum BaseLineType { get { return baseLineType; } set { baseLineType = value; } }
        #endregion

        #region Constructor
        public GeneralSettings(string projectDefaultName, string projectDefaultLocalisation, string patientsDefaultLocalisation, string localizersDefaultLocalisation, PlotNameCorrectionTypeEnum plotNameAutomaticCorrectionType, TrialMatrixSmoothingEnum trialMatrixSmoothingType,BaseLineTypeEnum baseLineType)
        {
            DefaultProjectName = projectDefaultName;
            DefaultProjectLocation = projectDefaultLocalisation;
            DefaultPatientDatabaseLocation = patientsDefaultLocalisation;
            DefaultLocalizerDatabaseLocation = DefaultLocalizerDatabaseLocation;
            PlotNameAutomaticCorrectionType = plotNameAutomaticCorrectionType;
            TrialMatrixSmoothingType = trialMatrixSmoothingType;
            BaseLineType = baseLineType;
        }
        public GeneralSettings() : this("New project", "", "", "",PlotNameCorrectionTypeEnum.None, TrialMatrixSmoothingEnum.None, BaseLineTypeEnum.None)
        {
        }
        #endregion

        #region Public Methods
        public void SaveXML()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GeneralSettings));
            TextWriter textWriter = new StreamWriter(PATH);
            serializer.Serialize(textWriter, this);
            textWriter.Close();
        }
        public void SaveJSon()
        {
            string l_json = JsonUtility.ToJson(this, true);
            using (StreamWriter outPutFile = new StreamWriter(PATH))
            {
                outPutFile.Write(l_json);
            }
        }
        public static GeneralSettings LoadXML()
        {
            GeneralSettings result = new GeneralSettings();
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GeneralSettings));
                TextReader textReader = new StreamReader(PATH);
                result = serializer.Deserialize(textReader) as GeneralSettings;
                textReader.Close();
            }
            catch
            {
                Debug.LogWarning("Can't Load the GeneralSettings file");
            }
            return result;
        }
        public static GeneralSettings LoadJson()
        {
            GeneralSettings result = new GeneralSettings();
            try
            {
                using (StreamReader inPutFile = new StreamReader(PATH))
                {
                    result = JsonUtility.FromJson<GeneralSettings>(inPutFile.ReadToEnd());
                }
            }
            catch
            {
                Debug.LogWarning("Can't Load the GeneralSettings file");
            }
            return result;
        }
        #endregion

        #region Private Methods
        string GenerateSavePath(string path)
        {
            return path + Path.DirectorySeparatorChar + "GeneralSettings.txt";
        }
        #endregion
    }
}

