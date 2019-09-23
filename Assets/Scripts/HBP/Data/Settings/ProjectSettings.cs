using HBP.Data.Preferences;
using HBP.Data.Tags;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data.General
{
    [DataContract]
    public class ProjectSettings : BaseData
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
        /// <summary>
        /// Aliases.
        /// </summary>
        [DataMember] public List<Alias> Aliases { get; set; }
        public ReadOnlyCollection<Tag> Tags
        {
            get
            {
                List<Tag> tags = new List<Tag>();
                tags.AddRange(GeneralTags);
                tags.AddRange(PatientsTags);
                tags.AddRange(SitesTags);
                return new ReadOnlyCollection<Tag>(tags);
            }
        }
        [DataMember] public List<Tag> GeneralTags { get; set; }
        [DataMember] public List<Tag> PatientsTags { get; set; }
        [DataMember] public List<Tag> SitesTags { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectSettings(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases, IEnumerable<Tag> generalTags, IEnumerable<Tag> patientsTags, IEnumerable<Tag> sitesTags, string ID) : base(ID)
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public ProjectSettings(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases, IEnumerable<Tag> generalTags, IEnumerable<Tag> patientsTags, IEnumerable<Tag> sitesTags) : base()
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public ProjectSettings(string name, string patientDatabase, string localizerDatabase) : this(name, patientDatabase, localizerDatabase, new Alias[0], new Tag[0], new Tag[0], new Tag[0])
        {
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectSettings(string name) : this(name, ApplicationState.UserPreferences.General.Project.DefaultPatientDatabase, ApplicationState.UserPreferences.General.Project.DefaultLocalizerDatabase, new Alias[0], new Tag[0], new Tag[0], new Tag[0])
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectSettings() : this(ApplicationState.UserPreferences.General.Project.DefaultName)
        {
        }
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var alias in Aliases) alias.GenerateID();
            foreach (var tag in GeneralTags) tag.GenerateID();
            foreach (var tag in PatientsTags) tag.GenerateID();
            foreach (var tag in SitesTags) tag.GenerateID();
        }
        #endregion

        #region Private Methods
        public override object Clone()
        {
            return new ProjectSettings(Name, PatientDatabase, LocalizerDatabase, Aliases.DeepClone(), GeneralTags.DeepClone(), PatientsTags.DeepClone(), SitesTags.DeepClone());
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProjectSettings projectSettings)
            {
                Name = projectSettings.Name;
                PatientDatabase = projectSettings.PatientDatabase;
                LocalizerDatabase = projectSettings.LocalizerDatabase;
                Aliases = projectSettings.Aliases;
                GeneralTags = projectSettings.GeneralTags;
                PatientsTags = projectSettings.PatientsTags;
                SitesTags = projectSettings.SitesTags;
            }
        }
        #endregion
    }
}