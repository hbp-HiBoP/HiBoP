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
        /// <summary>
        /// Aliases.
        /// </summary>
        [DataMember] public List<Alias> Aliases { get; set; }
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
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases, string ID) : base(ID)
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
        }
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase, IEnumerable<Alias> aliases) : base()
        {
            Name = name;
            PatientDatabase = patientDatabase;
            LocalizerDatabase = localizerDatabase;
            Aliases = aliases.ToList();
        }
        public ProjectPreferences(string name, string patientDatabase, string localizerDatabase) : this(name, patientDatabase, localizerDatabase, new Alias[2] { new Alias("[ANATOMICAL_DATABASE]", patientDatabase), new Alias("[FUNCTIONAL_DATABASE]", localizerDatabase) })
        {
        }
        /// <summary>
        /// Create a new project settings instance.
        /// </summary>
        /// <param name="name">Name of the project.</param>
        public ProjectPreferences(string name) : this(name, DefaultPatientDatabase, DefaultLocalizerDatabase, new Alias[0])
        {
        }
        /// <summary>
        /// Create a new project settings instance with default value.
        /// </summary>
        public ProjectPreferences() : this(DefaultName)
        {
        }
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var alias in Aliases) alias.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var alias in Aliases) IDs.AddRange(alias.GetAllIdentifiable());
            return IDs;
        }
        #endregion

        #region Private Methods
        public override object Clone()
        {
            return new ProjectPreferences(Name, PatientDatabase, LocalizerDatabase, Aliases.DeepClone(), ID);
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
            }
        }
        #endregion
    }
}