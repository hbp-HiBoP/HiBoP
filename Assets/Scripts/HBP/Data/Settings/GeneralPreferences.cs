using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class GeneralPreferences
    {
        [DataMember] public ProjectPreferences Project { get; set; }
        [DataMember] public ThemePreferences Theme { get; set; }
        [DataMember] public LocalizationPreferences Localization { get; set; }
        [DataMember] public SystemPreferences System { get; set; }
        [DataMember] public ExportPreferences Export { get; set; }
    }
    [DataContract]
    public class ProjectPreferences
    {
        [DataMember] public string DefaultName { get; set; }
        [DataMember] public string DefaultLocation { get; set; }
        [DataMember] public string DefaultPatientDatabase { get; set; }
        [DataMember] public string DefaultLocalizerDatabase { get; set; }
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
        [DataMember] public bool MultiThreading { get; set; }
    }
    [DataContract]
    public class ExportPreferences
    {
        [DataMember] public string DefaultScreenshotsLocation { get; set; }
    }
}