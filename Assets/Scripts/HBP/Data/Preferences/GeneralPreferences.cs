using Newtonsoft.Json;
using System;

namespace HBP.Data.Preferences
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GeneralPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public ProjectPreferences Project { get; set; }
        [JsonProperty] public ThemePreferences Theme { get; set; }
        [JsonProperty] public LocationPreferences Location { get; set; }
        [JsonProperty] public SystemPreferences System { get; set; }
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

    [JsonObject(MemberSerialization.OptIn)]
    public class ProjectPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public string DefaultName
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
        [JsonProperty] public string DefaultLocation { get; set; }
        [JsonProperty] public string DefaultPatientDatabase
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
        [JsonProperty] public string DefaultLocalizerDatabase
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
        [JsonProperty] public string DefaultExportLocation { get; set; }
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

    [JsonObject(MemberSerialization.OptIn)]
    public class ThemePreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new ThemePreferences();
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LocationPreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new LocationPreferences();
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class SystemPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool MultiThreading { get; set; }
        [JsonProperty] public int MemoryCacheLimit { get; set; }
        [JsonProperty] public int SleepModeAfter { get; set; }
        [JsonProperty] public int TargetFramerate { get; set; }
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