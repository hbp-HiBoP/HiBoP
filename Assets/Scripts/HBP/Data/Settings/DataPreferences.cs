using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class DataPreferences
    {
        [DataMember] public EEGPreferences EEG { get; set; }
        [DataMember] public EventPreferences Event { get; set; }
        [DataMember] public AnatomyPreferences Anatomy { get; set; }
    }

    [DataContract]
    public class EEGPreferences
    {
        [DataMember] public AveragingType Averaging { get; set; }
        [DataMember] public NormalizationType Normalization { get; set; }
    }

    [DataContract]
    public class EventPreferences
    {
        [DataMember] public AveragingType PositionAveraging { get; set; }
    }

    [DataContract]
    public class AnatomyPreferences
    {
        [DataMember] public bool SiteNameCorrection { get; set; }
        [DataMember] public bool PreloadMeshes { get; set; }
        [DataMember] public bool PreloadMRIs { get; set; }
        [DataMember] public bool PreloadImplantations { get; set; }
    }
}