using System;
using System.Runtime.Serialization;
using HBP.Data.Enums;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class DataPreferences : ICloneable
    {
        #region Properties
        [DataMember] public EEGPreferences EEG { get; set; }
        [DataMember] public ProtocolPreferences Protocol { get; set; }
        [DataMember] public AnatomicPreferences Anatomic { get; set; }
        #endregion

        #region Constructors
        public DataPreferences() : this(new EEGPreferences(), new ProtocolPreferences(), new AnatomicPreferences())
        {

        }
        public DataPreferences(EEGPreferences EEGPreferences, ProtocolPreferences protocolPreferences, AnatomicPreferences anatomicPreferences)
        {
            EEG = EEGPreferences;
            Protocol = protocolPreferences;
            Anatomic = anatomicPreferences;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new DataPreferences(EEG.Clone() as EEGPreferences, Protocol.Clone() as ProtocolPreferences, Anatomic.Clone() as AnatomicPreferences);
        }
        #endregion
    }

    [DataContract]
    public class EEGPreferences : ICloneable
    {
        #region Properties
        [DataMember] public AveragingType Averaging { get; set; }
        [DataMember] public NormalizationType Normalization { get; set; }
        [DataMember] public float CorrelationAlpha { get; set; }
        [DataMember] public bool BonferroniCorrection { get; set; }
        #endregion

        #region Constructors
        public EEGPreferences() : this(AveragingType.Median, NormalizationType.None, 0.05f, true)
        {

        }
        public EEGPreferences(AveragingType averaging, NormalizationType normalization, float correlationAlpha, bool bonferroniCorrection)
        {
            Averaging = averaging;
            Normalization = normalization;
            CorrelationAlpha = correlationAlpha;
            BonferroniCorrection = bonferroniCorrection;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new EEGPreferences(Averaging, Normalization, CorrelationAlpha, BonferroniCorrection);
        }
        #endregion
    }

    [DataContract]
    public class ProtocolPreferences : ICloneable
    {
        #region Properties
        [DataMember] public AveragingType PositionAveraging { get; set; }
        [DataMember] public float MinLimit { get; set; }
        [DataMember] public float MaxLimit { get; set; }
        [DataMember] public int Step { get; set; }
        #endregion

        #region Constructors
        public ProtocolPreferences() : this(AveragingType.Median, -3000, 3000, 0)
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

        #region Public Methods
        public object Clone()
        {
            return new ProtocolPreferences(PositionAveraging, MinLimit, MaxLimit, Step);
        }
        #endregion
    }

    [DataContract]
    public class AnatomicPreferences : ICloneable
    {
        #region Properties
        [DataMember] public bool SiteNameCorrection { get; set; }
        [DataMember] public bool MeshPreloading { get; set; }
        [DataMember] public bool MRIPreloading { get; set; }
        [DataMember] public bool ImplantationPreloading { get; set; }
        #endregion

        #region Constructors
        public AnatomicPreferences() : this(true, true, true, true)
        {

        }
        public AnatomicPreferences(bool siteNameCorrection, bool meshPreloading, bool mriPreloading, bool implantationPreloading)
        {
            SiteNameCorrection = siteNameCorrection;
            MeshPreloading = meshPreloading;
            MRIPreloading = mriPreloading;
            ImplantationPreloading = implantationPreloading;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new AnatomicPreferences(SiteNameCorrection, MeshPreloading, MRIPreloading, ImplantationPreloading);
        }
        #endregion
    }
}