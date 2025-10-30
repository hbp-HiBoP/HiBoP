using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine.Scripting;

namespace HBP.Data.Preferences
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
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

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class ProjectPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public string DefaultName { get; set; }
        [JsonProperty] public string DefaultLocation { get; set; }
        [JsonProperty] public string DefaultExportLocation { get; set; }
        #endregion

        #region Constructors
        public ProjectPreferences() : this("New Project", "", "")
        {

        }
        public ProjectPreferences(string defaultName, string defaultLocation, string defaultExportLocation)
        {
            DefaultName = defaultName;

            if (string.IsNullOrEmpty(defaultLocation))
                DefaultLocation = GetDefaultPath("Projects");
            else
                DefaultLocation = defaultLocation;

            if (string.IsNullOrEmpty(defaultExportLocation))
                DefaultExportLocation = GetDefaultPath("Exports");
            else
                DefaultExportLocation = defaultExportLocation;

            Directory.CreateDirectory(DefaultLocation);
            Directory.CreateDirectory(DefaultExportLocation);
        }
        #endregion

        #region Private Methods
        private static string GetDefaultPath(string subfolder)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                string home = Environment.GetEnvironmentVariable("HOME") ?? "";

                string nextcloudPath = Path.Combine(home, "nextcloud");
                if (Directory.Exists(nextcloudPath))
                    return Path.Combine(nextcloudPath, "HiBoP", subfolder);
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(documentsPath))
            {
                string home = Environment.GetEnvironmentVariable("HOME") ?? "";
                documentsPath = Path.Combine(home, "Documents");
            }

            return Path.Combine(documentsPath, "HiBoP", subfolder);
        }

        #endregion

        #region Public Methods
        public object Clone()
        {
            return new ProjectPreferences(DefaultName, DefaultLocation, DefaultExportLocation);
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class ThemePreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new ThemePreferences();
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class LocationPreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new LocationPreferences();
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
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