using System.Runtime.Serialization;
using HBP.Data.Enums;
using UnityEngine;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class DataPreferences
    {
        #region Properties
        [DataMember] public EEGPrefrences EEG { get; set; }
        [DataMember] public ProtocolPreferences Protocol { get; set; }
        [DataMember] public AnatomicPreferences Anatomic { get; set; }
        #endregion

        #region Constructors
        public DataPreferences()
        {
            EEG = new EEGPrefrences();
            Protocol = new ProtocolPreferences();
            Anatomic = new AnatomicPreferences();
        }
        #endregion
    }

    [DataContract]
    public class EEGPrefrences
    {
        #region Properties
        [DataMember] public AveragingType Averaging { get; set; }
        [DataMember] public NormalizationType Normalization { get; set; }
        #endregion

        #region Constructors
        public EEGPrefrences()
        {
            Averaging = AveragingType.Median;
            Normalization =  NormalizationType.None;
        }
        #endregion
    }

    [DataContract]
    public class ProtocolPreferences
    {
        #region Properties
        [DataMember] public AveragingType PositionAveraging { get; set; }
        [DataMember] public float MinLimit { get; set; }
        [DataMember] public float MaxLimit{ get; set; }
        [DataMember] public int Step { get; set; }
        #endregion

        #region Constructors
        public ProtocolPreferences() : this(AveragingType.Median, -1500, 1500, 50)
        {
        }
        public ProtocolPreferences(AveragingType positionAveraging, float minLimit, float maxLimit, int step)
        {
            PositionAveraging = positionAveraging;
            MinLimit = minLimit;
            MaxLimit = maxLimit;
            Step = step;
        }
        #endregion
    }

    [DataContract]
    public class AnatomicPreferences
    {
        #region Properties
        [DataMember] public bool SiteNameCorrection { get; set; }
        [DataMember] public bool MeshPreloading { get; set; }
        [DataMember] public bool MRIPreloading { get; set; }
        [DataMember] public bool ImplantationPreloading { get; set; }
        #endregion

        #region Constructors
        public AnatomicPreferences(bool siteNameCorrection, bool meshPreloading, bool mRIPreloading, bool implantationPreloading)
        {
            SiteNameCorrection = siteNameCorrection;
            MeshPreloading = meshPreloading;
            MRIPreloading = mRIPreloading;
            ImplantationPreloading = implantationPreloading;
        }
        public AnatomicPreferences() : this(true,false,false,false)
        {
        }
        #endregion
    }
}