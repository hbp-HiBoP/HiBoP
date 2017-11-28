using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using HBP.UI.Theme;

namespace HBP.Data.Settings
{
    /**
    * \class GeneralSettings
    * \author Adrien Gannerie
    * \version 1.0
    * \date 16 janvier 2017
    * \brief Settings of the application.
    * 
    * \details Class which contains the settings of the application:
    *     - default name of a project.
    *     - default localisation of a project.
    *     - default localisation of the patient database.
    *     - default localisation of the localizer database.
    *     - Option : Plot name automatic correction.
    *     - Option : Smoothing type of the trial matrix.
    *     - Option : Baseline of the trial matrix.
    */
    [DataContract]
    public class GeneralSettings
    {
        #region Properties
        public static string PATH = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "GeneralSettings.txt";
        public enum PlotNameCorrectionTypeEnum { Disable, Enable }
        public enum AveragingMode { Mean, Median }

        private Theme[] m_Themes;
        /// <summary>
        /// List of the themes.
        /// </summary>
        public Theme[] Themes
        {
            get
            {
                return m_Themes;
            }
        }

        /// <summary>
        /// Default project name.
        /// </summary>
        [DataMember]
        public string DefaultProjectName { get; set; }
        /// <summary>
        /// Default project location.
        /// </summary>
        [DataMember]
        public string DefaultProjectLocation { get; set; }
        /// <summary>
        /// Default patient database location.
        /// </summary>
        [DataMember]
        public string DefaultPatientDatabaseLocation { get; set; }
        /// <summary>
        /// Default localizer database location.
        /// </summary>
        [DataMember]
        public string DefaultLocalizerDatabaseLocation { get; set; }
        /// <summary>
        /// Default screenshots location.
        /// </summary>
        [DataMember]
        public string DefaultScreenshotsLocation { get; set; }
        /// <summary>
        /// Active or Deactive the plot name automatic correction (cast, p/' , etc...)
        /// </summary>
        [DataMember]
        public PlotNameCorrectionTypeEnum PlotNameAutomaticCorrectionType { get; set; }
        /// <summary>
        /// Bloc event position averaging.
        /// </summary>
        [DataMember]
        public AveragingMode EventPositionAveraging { get; set; }
        /// <summary>
        /// Bloc value averaging.
        /// </summary>
        [DataMember]
        public AveragingMode ValueAveraging { get; set; }
        /// <summary>
        /// Settings of the trial matrix.
        /// </summary>
        [DataMember]
        public TrialMatrixSettings TrialMatrixSettings { get; set; }

        [IgnoreDataMember]
        private string m_ThemeName;
        /// <summary>
        /// Name of the used theme.
        /// </summary>
        [DataMember(Name = "Theme")]
        public string ThemeName
        {
            get
            {
                return m_ThemeName;
            }
            set
            {
                m_ThemeName = value;
                if (Themes.Length == 0)
                {
                    UI.Theme.Theme defaultTheme = new UI.Theme.Theme();
                    defaultTheme.SetDefaultValues();
                    m_Theme = defaultTheme;
                }
                else
                {
                    UI.Theme.Theme theme = Themes.FirstOrDefault((t) => t.name == ThemeName);
                    if (theme)
                    {
                        m_Theme = theme;
                    }
                    else
                    {
                        m_Theme = Resources.Load("Themes/Dark") as Theme;
                    }
                }
                UI.Theme.Theme.UpdateThemeElements(m_Theme);
            }
        }

        /// <summary>
        /// Display the cut lines on the cuts
        /// </summary>
        [DataMember]
        public bool ShowCutLines { get; set; }

        [IgnoreDataMember]
        private UI.Theme.Theme m_Theme;
        /// <summary>
        /// Used theme.
        /// </summary>
        [IgnoreDataMember]
        public UI.Theme.Theme Theme
        {
            get
            {
                return m_Theme;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new general settings instance.
        /// </summary>
        /// <param name="projectDefaultName">Default project name.</param>
        /// <param name="defaultProjectLocation">Default project location.</param>
        /// <param name="defaultPatientDatabseLocation">Default patient database location.</param>
        /// <param name="defaultLocalizerDatabaseLocation">Default localizer database location.</param>
        /// <param name="plotNameAutomaticCorrectionType">Plot name automatic correction.</param>
        /// <param name="trialMatrixSmoothingType">Trial maltrix smoothing.</param>
        /// <param name="BaselineType">Trial matrix Baseline.</param>
        public GeneralSettings(string projectDefaultName,string defaultProjectLocation, string defaultPatientDatabseLocation, string defaultLocalizerDatabaseLocation, string defaultScreenshotsLocation, PlotNameCorrectionTypeEnum plotNameAutomaticCorrectionType,TrialMatrixSettings trialMatrixSettings, string themeName)
        {
            DefaultProjectName = projectDefaultName;
            DefaultProjectLocation = defaultProjectLocation;
            DefaultPatientDatabaseLocation = defaultPatientDatabseLocation;
            DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocation;
            DefaultScreenshotsLocation = defaultScreenshotsLocation;
            PlotNameAutomaticCorrectionType = plotNameAutomaticCorrectionType;
            TrialMatrixSettings = trialMatrixSettings;
            // TODO : include all themes in a specific folder
            m_Themes = Resources.LoadAll<Theme>("Themes");
            ThemeName = themeName;
        }
        /// <summary>
        /// Create a new general settings instance with default values.
        /// </summary>
        public GeneralSettings() : this("New project","","","", Path.GetFullPath(Application.dataPath + "/../Screenshots/"), PlotNameCorrectionTypeEnum.Enable,new TrialMatrixSettings(), "")
        {
        }
        #endregion
    }
}