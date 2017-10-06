using UnityEngine;
using System.IO;

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
    *     - Option : BaseLine of the trial matrix.
    */
	public class GeneralSettings
	{
        #region Properties
        public static string PATH = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "GeneralSettings.txt";
        public enum PlotNameCorrectionTypeEnum { Disable, Enable }
        public enum AveragingMode { Mean , Median }

        /// <summary>
        /// Default project name.
        /// </summary>
        public string DefaultProjectName { get; set; }
        /// <summary>
        /// Default project location.
        /// </summary>
        public string DefaultProjectLocation { get; set; }
        /// <summary>
        /// Default patient database location.
        /// </summary>
        public string DefaultPatientDatabaseLocation { get; set; }
        /// <summary>
        /// Default localizer database location.
        /// </summary>
        public string DefaultLocalizerDatabaseLocation { get; set; }
        /// <summary>
        /// Active or Deactive the plot name automatic correction (cast, p/' , etc...)
        /// </summary>
        public PlotNameCorrectionTypeEnum PlotNameAutomaticCorrectionType { get; set; }
        /// <summary>
        /// Bloc event position averaging.
        /// </summary>
        public AveragingMode EventPositionAveraging { get; set; }
        /// <summary>
        /// Bloc value averaging.
        /// </summary>
        public AveragingMode ValueAveraging { get; set; }
        /// <summary>
        /// Settings of the trial matrix.
        /// </summary>
        public TrialMatrixSettings TrialMatrixSettings { get; set; }
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
        /// <param name="baseLineType">Trial matrix baseline.</param>
        public GeneralSettings(string projectDefaultName,string defaultProjectLocation, string defaultPatientDatabseLocation, string defaultLocalizerDatabaseLocation, PlotNameCorrectionTypeEnum plotNameAutomaticCorrectionType,TrialMatrixSettings trialMatrixSettings)
        {
            DefaultProjectName = projectDefaultName;
            DefaultProjectLocation = defaultProjectLocation;
            DefaultPatientDatabaseLocation = defaultPatientDatabseLocation;
            DefaultLocalizerDatabaseLocation = defaultLocalizerDatabaseLocation;
            PlotNameAutomaticCorrectionType = plotNameAutomaticCorrectionType;
            TrialMatrixSettings = trialMatrixSettings;
        }
        /// <summary>
        /// Create a new general settings instance with default values.
        /// </summary>
        public GeneralSettings() : this("New project","","","",PlotNameCorrectionTypeEnum.Enable,new TrialMatrixSettings())
        {
        }
        #endregion
    }
}