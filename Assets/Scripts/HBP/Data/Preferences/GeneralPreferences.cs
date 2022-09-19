using System;
using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class GeneralPreferences : ICloneable
    {
        #region Properties
        [DataMember] public ProjectPreferences Project { get; set; }
        [DataMember] public ThemePreferences Theme { get; set; }
        [DataMember] public LocationPreferences Location { get; set; }
        [DataMember] public SystemPreferences System { get; set; }
        #endregion

        #region Constructors
        public GeneralPreferences() : this(new ProjectPreferences(), new ThemePreferences(), new LocationPreferences(), new SystemPreferences())
        {

        }
        public GeneralPreferences(ProjectPreferences project, ThemePreferences theme, LocationPreferences location, SystemPreferences system)
        {
            Project = project;
            Theme = theme;
            Location = location;
            System = system;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new GeneralPreferences(Project.Clone() as ProjectPreferences, Theme.Clone() as ThemePreferences, Location.Clone() as LocationPreferences, System.Clone() as SystemPreferences);
        }
        #endregion
    }

    [DataContract]
    public class ProjectPreferences : ICloneable
    {
        #region Properties
        [DataMember] public string DefaultName
        {
            get
            {
                return Core.Data.ProjectPreferences.DefaultName;
            }
            set
            {
                Core.Data.ProjectPreferences.DefaultName = value;
            }
        }
        [DataMember] public string DefaultLocation { get; set; }
        [DataMember] public string DefaultPatientDatabase
        {
            get
            {
                return Core.Data.ProjectPreferences.DefaultPatientDatabase;
            }
            set
            {
                Core.Data.ProjectPreferences.DefaultPatientDatabase = value;
            }
        }
        [DataMember] public string DefaultLocalizerDatabase
        {
            get
            {
                return Core.Data.ProjectPreferences.DefaultLocalizerDatabase;
            }
            set
            {
                Core.Data.ProjectPreferences.DefaultLocalizerDatabase = value;
            }
        }
        [DataMember] public string DefaultExportLocation { get; set; }
        #endregion

        #region Constructors
        public ProjectPreferences() : this("New Project","","","","")
        {

        }
        public ProjectPreferences(string defaultName, string defaultLocation, string defaultPatientDatabase, string defaultLocalizerDatabase, string defaultExportLocation)
        {
            DefaultName = defaultName;
            DefaultLocation = defaultLocation;
            DefaultPatientDatabase = defaultPatientDatabase;
            DefaultLocalizerDatabase = defaultLocalizerDatabase;
            DefaultExportLocation = defaultExportLocation;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new ProjectPreferences(DefaultName, DefaultLocation, DefaultPatientDatabase, DefaultLocalizerDatabase, DefaultExportLocation);
        }
        #endregion
    }

    [DataContract]
    public class ThemePreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new ThemePreferences();
        }
        #endregion
    }
    [DataContract]
    public class LocationPreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new LocationPreferences();
        }
        #endregion
    }
    [DataContract]
    public class SystemPreferences : ICloneable
    {
        #region Properties
        [DataMember] public bool MultiThreading { get; set; }
        [DataMember] public int MemoryCacheLimit { get; set; }
        [DataMember] public int SleepModeAfter { get; set; }
        [DataMember] public int TargetFramerate { get; set; }
        #endregion

        #region Constructors
        public SystemPreferences() : this(true, 0, 1, 60)
        {

        }
        public SystemPreferences(bool multiThreading, int memoryCacheLimit, int sleepModeAfter, int targetFramerate)
        {
            MultiThreading = multiThreading;
            MemoryCacheLimit = memoryCacheLimit;
            SleepModeAfter = sleepModeAfter;
            TargetFramerate = targetFramerate;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SystemPreferences(MultiThreading, MemoryCacheLimit, SleepModeAfter, TargetFramerate);
        }
        #endregion
    }
}