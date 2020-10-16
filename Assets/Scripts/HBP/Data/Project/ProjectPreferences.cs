using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data
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
        /// <summary>
        /// Aliases.
        /// </summary>
        [DataMember] public List<Alias> Aliases { get; set; }
        public ReadOnlyCollection<BaseTag> Tags
        {
            get
            {
                List<BaseTag> tags = new List<BaseTag>();
                tags.AddRange(GeneralTags);
                tags.AddRange(PatientsTags);
                tags.AddRange(SitesTags);
                return new ReadOnlyCollection<BaseTag>(tags);
            }
        }
        [DataMember] public List<BaseTag> GeneralTags { get; set; }
        [DataMember] public List<BaseTag> PatientsTags { get; set; }
        [DataMember] public List<BaseTag> SitesTags { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="patientDatabase">Patient database of the project.</param>
        /// <param name="localizerDatabase">Localizer database of the project.</param>
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases, IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags, string ID) : base(ID)
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases, IEnumerable<BaseTag> generalTags, IEnumerable<BaseTag> patientsTags, IEnumerable<BaseTag> sitesTags) : base()
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
            GeneralTags = generalTags.ToList();
            PatientsTags = patientsTags.ToList();
            SitesTags = sitesTags.ToList();
        }
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase) : this(name, patientDatabase, localizerDatabase, new Alias[2] { new Alias("[ANATOMICAL_DATABASE]", patientDatabase), new Alias("[FUNCTIONAL_DATABASE]", localizerDatabase) }, new BaseTag[0], new BaseTag[0], new BaseTag[0])
        {
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectPreferences(string name) : this(name, ApplicationState.UserPreferences.General.Project.DefaultPatientDatabase, ApplicationState.UserPreferences.General.Project.DefaultLocalizerDatabase, new Alias[0], new BaseTag[0], new BaseTag[0], new BaseTag[0])
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectPreferences() : this(ApplicationState.UserPreferences.General.Project.DefaultName)
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
            return new ProjectPreferences(Name, PatientDatabase, LocalizerDatabase, Aliases.DeepClone(), GeneralTags.DeepClone(), PatientsTags.DeepClone(), SitesTags.DeepClone());
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ProjectPreferences projectSettings)
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