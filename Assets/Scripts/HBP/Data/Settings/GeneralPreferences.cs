using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class GeneralPreferences
    {
        #region Properties
        [DataMember] public ProjectPreferences Project { get; set; }
        [DataMember] public ThemePreferences Theme { get; set; }
        [DataMember] public LocalizationPreferences Localization { get; set; }
        [DataMember] public SystemPreferences System { get; set; }
        #endregion

        #region Constructors
        public GeneralPreferences()
        {
            Project = new ProjectPreferences();
            Theme = new ThemePreferences();
            Localization = new LocalizationPreferences();
            System = new SystemPreferences();
        }
        #endregion
    }
    [DataContract]
    public class ProjectPreferences
    {
        #region Properties
        [DataMember] public string DefaultName { get; set; }
        [DataMember] public string DefaultLocation { get; set; }
        [DataMember] public string DefaultPatientDatabase { get; set; }
        [DataMember] public string DefaultLocalizerDatabase { get; set; }
        [DataMember] public string DefaultExportLocation { get; set; }
        #endregion

        #region Constructors
        public ProjectPreferences()
        {
            DefaultName = "New project";
            DefaultLocation = string.Empty;
            DefaultPatientDatabase = string.Empty;
            DefaultLocalizerDatabase = string.Empty;
            DefaultExportLocation = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Export");
        }
        #endregion
    }
    [DataContract]
    public class ThemePreferences
    {

    }
    [DataContract]
    public class LocalizationPreferences
    {

    }
    [DataContract]
    public class SystemPreferences
    {
        #region Properties
        [DataMember] public bool MultiThreading { get; set; }
        [DataMember] public int MemoryCacheLimit { get; set; }
        #endregion

        #region Constructors
        public SystemPreferences()
        {
            MultiThreading = true;
            MemoryCacheLimit = UnityEngine.SystemInfo.systemMemorySize;
        }
        #endregion
    }
}