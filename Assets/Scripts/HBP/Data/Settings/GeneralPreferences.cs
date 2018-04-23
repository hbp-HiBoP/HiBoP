using System.Runtime.Serialization;

[DataContract]
public struct GeneralPreferences
{
    [DataMember] public ProjectPreferences Project { get; set; }
    [DataMember] public ThemePreferences Theme { get; set; }
    [DataMember] public LocalizationPreferences Localization { get; set; }
    [DataMember] public SystemPreferences System { get; set; }
    [DataMember] public ExportPreferences Export { get; set; }
}
[DataContract]
public struct ProjectPreferences
{
    [DataMember] public string DefaultName { get; set; }
    [DataMember] public string DefaultLocation { get; set; }
    [DataMember] public string DefaultPatientDatabase { get; set; }
    [DataMember] public string DefaultLocalizerDatabase { get; set; }
}
[DataContract]
public struct ThemePreferences
{

}
[DataContract]
public struct LocalizationPreferences
{

}
[DataContract]
public struct SystemPreferences
{
    [DataMember] public bool MultiThreading { get; set; }
}
[DataContract]
public struct ExportPreferences
{
    [DataMember] public string DefaultScreenshotsLocation { get; set; }
}
