using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class DataPreferences
    {
        #region Properties
        [DataMember] public EEGPreferences EEG { get; set; }
        [DataMember] public EventPreferences Event { get; set; }
        [DataMember] public AnatomyPreferences Anatomy { get; set; }
        #endregion

        #region Constructors
        public DataPreferences()
        {
            EEG = new EEGPreferences();
            Event = new EventPreferences();
            Anatomy = new AnatomyPreferences();
        }
        #endregion
    }

    [DataContract]
    public class EEGPreferences
    {
        #region Properties
        [DataMember] public AveragingType Averaging { get; set; }
        [DataMember] public NormalizationType Normalization { get; set; }
        #endregion

        #region Constructors
        public EEGPreferences()
        {
            Averaging = AveragingType.Median;
            Normalization = NormalizationType.None;
        }
        #endregion
    }

    [DataContract]
    public class EventPreferences
    {
        #region Properties
        [DataMember] public AveragingType PositionAveraging { get; set; }
        #endregion

        #region Constructors
        public EventPreferences()
        {
            PositionAveraging = AveragingType.Median;
        }
        #endregion
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