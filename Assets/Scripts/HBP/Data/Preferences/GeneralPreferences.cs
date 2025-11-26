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
        [JsonProperty] public LocalizationPreferences Localization { get; set; }
        [JsonProperty] public SystemPreferences System { get; set; }
        [JsonProperty] public MiscPreferences Misc { get; set; }
        #endregion

        #region Constructors
        public GeneralPreferences() : this(new ProjectPreferences(), new ThemePreferences(), new LocalizationPreferences(), new SystemPreferences(), new MiscPreferences())
        {

        }
        public GeneralPreferences(ProjectPreferences project, ThemePreferences theme, LocalizationPreferences localization, SystemPreferences system, MiscPreferences misc)
        {
            Project = project;
            Theme = theme;
            Localization = localization;
            System = system;
            Misc = misc;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new GeneralPreferences(Project.Clone() as ProjectPreferences, Theme.Clone() as ThemePreferences, Localization.Clone() as LocalizationPreferences, System.Clone() as SystemPreferences, Misc.Clone() as MiscPreferences);
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
    public class LocalizationPreferences : ICloneable
    {
        #region Public Methods
        public object Clone()
        {
            return new LocalizationPreferences();
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

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class MiscPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool AdvancedFeatures { get; set; }
        #endregion

        #region Constructors
        public MiscPreferences() : this(false)
        {
        }
        public MiscPreferences(bool advancedFeatures)
        {
            AdvancedFeatures = advancedFeatures;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new MiscPreferences(AdvancedFeatures);
        }
        #endregion
    }
}