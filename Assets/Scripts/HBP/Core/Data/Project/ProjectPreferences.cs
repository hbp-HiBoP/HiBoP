using HBP.Data.Preferences;
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
        [JsonIgnore] public bool CanLoadProject = true;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectPreferences(string name, string ID) : base(ID)
        {
            Name = name;
        }
        public ProjectPreferences(string name) : base()
        {
            Name = name;
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectPreferences() : this(PersistentDataManager.UserPreferences.General.Project.DefaultName)
        {
        }
        #endregion

        #region Private Methods
        public override object Clone()
        {
            return new ProjectPreferences(Name, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProjectPreferences projectSettings)
            {
                Name = projectSettings.Name;
            }
        }
        #endregion
    }
}