using System;

namespace HBP.Data.Settings
{
    /**
    * \class ProjectSettings
    * \author Adrien Gannerie
    * \version 1.0
    * \date 16 janvier 2017
    * \brief Settings of a project.
    * 
    * \details Class which contains the settings of a project:
    *     - Name of the project.
    *     - Localisation of the project.
    *     - Localisation of the patients database.
    *     - Localisation of the localizer database.
    */
    public class ProjectSettings
    {
        #region Properties
        /// <summary>
        /// Project settings extension.
        /// </summary>
        public const string EXTENSION = ".settings";
        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Project settings name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Patient database.
        /// </summary>
        public string PatientDatabase { get; set; }
        /// <summary>
        /// Localizer database.
        /// </summary>
        public string LocalizerDatabase { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectSettings(string name, string patientDatabase, string localizerDatabase)
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            ID = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectSettings(string name) : this(name, ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation, ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation)
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectSettings() : this(ApplicationState.GeneralSettings.DefaultProjectName, ApplicationState.GeneralSettings.DefaultPatientDatabaseLocation, ApplicationState.GeneralSettings.DefaultLocalizerDatabaseLocation)
        {
        }
        #endregion
    }
}