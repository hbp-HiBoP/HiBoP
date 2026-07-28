using HBP.Core.Tools;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class ProjectPreferences : BaseData
    {
        #region Properties

        /// <summary>
        /// Project settings extension.
        /// </summary>
        public const string EXTENSION = ".settings";

        [JsonProperty] public string Version { get; set; }
        [JsonIgnore] public bool CanLoadProject = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectPreferences(string version, string ID) : base(ID)
        {
            Version = version;
        }

        public ProjectPreferences(string version) : base()
        {
            Version = version;
        }

        public ProjectPreferences() : this("Unknown")
        {
        }

        #endregion

        #region Private Methods

        public override object Clone()
        {
            return new ProjectPreferences(Version, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProjectPreferences projectSettings)
            {
                Version = projectSettings.Version;
            }
        }

        #endregion
    }
}
