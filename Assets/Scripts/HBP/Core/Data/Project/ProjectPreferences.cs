using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
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
        [JsonProperty] public string Name { get; set; }
        /// <summary>
        /// Patient database.
        /// </summary>
        [JsonProperty] public string PatientDatabase { get; set; }
        /// <summary>
        /// Localizer database.
        /// </summary>
        [JsonProperty] public string LocalizerDatabase { get; set; }
        [JsonIgnore] public static string DefaultName = "New Project";
        [JsonIgnore] public static string DefaultPatientDatabase = "";
        [JsonIgnore] public static string DefaultLocalizerDatabase = "";
        [JsonIgnore] public bool CanLoadProject = true;
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