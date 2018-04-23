using System.Runtime.Serialization;

[DataContract]
public struct DataPreferences
{
    [DataMember] public EEGPreferences EEG { get; set; }
    [DataMember] public EventPreferences Event { get; set; }
    [DataMember] public AnatomyPreferences Anatomy { get; set; }
}

[DataContract]
public struct EEGPreferences
{
    [DataMember] public AveragingType ValueAveraging { get; set; }
    [DataMember] public NormalizationType Normalization { get; set; }
}

[DataContract]
public struct EventPreferences
{
    [DataMember] public AveragingType PositionAveraging { get; set; }
}

[DataContract]
public struct  AnatomyPreferences
{
    [DataMember] public bool SiteNameCorrection { get; set; }
    [DataMember] public bool PreloadMeshes { get; set; }
    [DataMember] public bool PreloadMRIs { get; set; }
    [DataMember] public bool PreloadImplantations { get; set; }
}