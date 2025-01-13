using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    [DataContract]
    public class ProjectPreferences : BaseData
    {
        #region Properties
        /// <summary>
        /// Project settings extension.
        /// </summary>
        public const string EXTENSION = ".settings";
        /// <summary>
        /// Project settings name.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Patient database.
        /// </summary>
        [DataMember] public string PatientDatabase { get; set; }
        /// <summary>
        /// Localizer database.
        /// </summary>
        [DataMember] public string LocalizerDatabase { get; set; }
        [IgnoreDataMember] public static string DefaultName = "New Project";
        [IgnoreDataMember] public static string DefaultPatientDatabase = "";
        [IgnoreDataMember] public static string DefaultLocalizerDatabase = "";
        [IgnoreDataMember] public bool CanLoadProject = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase, string ID) : base(ID)
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
        }
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase) : base()
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectPreferences(string name) : this(name, DefaultPatientDatabase, DefaultLocalizerDatabase)
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectPreferences() : this(DefaultName)
        {
        }
        #endregion

        #region Private Methods
        public override object Clone()
        {
            return new ProjectPreferences(Name, PatientDatabase, LocalizerDatabase, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProjectPreferences projectSettings)
            {
                Name = projectSettings.Name;
                PatientDatabase = projectSettings.PatientDatabase;
                LocalizerDatabase = projectSettings.LocalizerDatabase;
            }
        }
        #endregion
    }
}