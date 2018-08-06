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
        [DataMember] public Enums.AveragingType Averaging { get; set; }
        [DataMember] public Enums.NormalizationType Normalization { get; set; }
        #endregion

        #region Constructors
        public EEGPreferences()
        {
            Averaging = Enums.AveragingType.Median;
            Normalization = Enums.NormalizationType.None;
        }
        #endregion
    }

    [DataContract]
    public class EventPreferences
    {
        #region Properties
        [DataMember] public Enums.AveragingType PositionAveraging { get; set; }
        #endregion

        #region Constructors
        public EventPreferences()
        {
            PositionAveraging = Enums.AveragingType.Median;
        }
        #endregion
    }

    [DataContract]
    public class AnatomyPreferences
    {
        [DataMember] public bool SiteNameCorrection { get; set; }
        [DataMember] public bool MeshPreloading { get; set; }
        [DataMember] public bool MRIPreloading { get; set; }
        [DataMember] public bool ImplantationPreloading { get; set; }
    }
}