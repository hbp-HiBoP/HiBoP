using System;
using System.Collections.Generic;

namespace HBP.Data.Preferences
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
        /// <summary>
        /// Aliases.
        /// </summary>
        public List<Alias> Aliases { get; set; }
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
            Aliases = new List<Alias>();
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectSettings(string name) : this(name, ApplicationState.UserPreferences.General.Project.DefaultPatientDatabase, ApplicationState.UserPreferences.General.Project.DefaultLocalizerDatabase)
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectSettings() : this(ApplicationState.UserPreferences.General.Project.DefaultName)
        {
        }
        #endregion
    }
}