using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using HBP.UI.Theme;

namespace HBP.Data.Settings
{
    /**
    * \class UserPreferences
    * \author Adrien Gannerie
    * \version 1.0
    * \date 16 janvier 2017
    * \brief User preferences.
    * 
    * \details Class which contains the user preferences:
    *     - General preferences.
    *     - Data preferences.
    *     - Visualization preferences.
    */
    [DataContract]
    public class UserPreferences
    {
        #region Properties
        public static string PATH = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "Preferences.txt";

        [DataMember] public GeneralPreferences General { get; set; }
        [DataMember] public DataPreferences Data { get; set; }
        [DataMember] public VisaluzationPreferences Visualization { get; set; }

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

        [IgnoreDataMember] private string m_ThemeName;
        /// <summary>
        /// Name of the used theme.
        /// </summary>
        [DataMember(Name = "Theme")] public string ThemeName
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
                    Theme defaultTheme = new Theme();
                    defaultTheme.SetDefaultValues();
                    Theme = defaultTheme;
                }
                else
                {
                    Theme theme = Themes.FirstOrDefault((t) => t.name == ThemeName);
                    if (theme)
                    {
                        Theme = theme;
                    }
                    else
                    {
                        Theme = Resources.Load("Themes/Dark") as Theme;
                    }
                }
                Theme.UpdateThemeElements(Theme);
            }
        }
        /// <summary>
        /// Used theme.
        /// </summary>
        [IgnoreDataMember] public Theme Theme { get; private set; }
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
        public UserPreferences(string projectDefaultName,string defaultProjectLocation, string defaultPatientDatabseLocation, string defaultLocalizerDatabaseLocation, string defaultScreenshotsLocation, PlotNameCorrectionTypeEnum plotNameAutomaticCorrectionType,TrialMatrixSettings trialMatrixSettings, bool hideCurvesWhenColumnClose, string themeName)
        {
            DefaultProjectName = projectDefaultName;
            DefaultProjectLocation = defaultProjectLocation;
            DefaultPatientDatabaseLocation = defaultPatientDatabseLocation;
            DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocation;
            DefaultScreenshotsLocation = defaultScreenshotsLocation;
            SiteNameCorrection = plotNameAutomaticCorrectionType;
            TrialMatrixSettings = trialMatrixSettings;
            HideCurveWhenColumnHidden = hideCurvesWhenColumnClose;
            // TODO : include all themes in a specific folder
            m_Themes = Resources.LoadAll<Theme>("Themes");
            ThemeName = themeName;
        }
        /// <summary>
        /// Create a new general settings instance with default values.
        /// </summary>
        public UserPreferences() : this("New project","","","", Path.GetFullPath(Application.dataPath + "/../Screenshots/"), PlotNameCorrectionTypeEnum.Enable,new TrialMatrixSettings(), true, "")
        {
        }
        #endregion
    }
}