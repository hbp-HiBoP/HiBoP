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
        [DataMember] public AtlasesPreferences Atlases { get; set; }
        #endregion

        #region Constructors
        public DataPreferences() : this(new EEGPreferences(), new ProtocolPreferences(), new AnatomicPreferences(), new AtlasesPreferences())
        {

        }
        public DataPreferences(EEGPreferences EEGPreferences, ProtocolPreferences protocolPreferences, AnatomicPreferences anatomicPreferences, AtlasesPreferences atlasesPreferences)
        {
            EEG = EEGPreferences;
            Protocol = protocolPreferences;
            Anatomic = anatomicPreferences;
            Atlases = atlasesPreferences;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new DataPreferences(EEG.Clone() as EEGPreferences, Protocol.Clone() as ProtocolPreferences, Anatomic.Clone() as AnatomicPreferences, Atlases.Clone() as AtlasesPreferences);
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
        [DataMember] public bool PreloadSinglePatientDataInMultiPatientVisualization { get; set; }
        #endregion

        #region Constructors
        public AnatomicPreferences() : this(true, false, false, false, false)
        {

        }
        public AnatomicPreferences(bool siteNameCorrection, bool meshPreloading, bool mriPreloading, bool implantationPreloading, bool preloadSinglePatientDataInMultiPatientVisualization)
        {
            SiteNameCorrection = siteNameCorrection;
            MeshPreloading = meshPreloading;
            MRIPreloading = mriPreloading;
            ImplantationPreloading = implantationPreloading;
            PreloadSinglePatientDataInMultiPatientVisualization = preloadSinglePatientDataInMultiPatientVisualization;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new AnatomicPreferences(SiteNameCorrection, MeshPreloading, MRIPreloading, ImplantationPreloading, PreloadSinglePatientDataInMultiPatientVisualization);
        }
        #endregion
    }

    [DataContract]
    public class AtlasesPreferences : ICloneable
    {
        #region Properties
        [DataMember] public bool PreloadMarsAtlas { get; set; }
        [DataMember] public bool PreloadJuBrain { get; set; }
        [DataMember] public bool PreloadIBC { get; set; }
        [DataMember] public bool PreloadDiFuMo64 { get; set; }
        [DataMember] public bool PreloadDiFuMo128 { get; set; }
        [DataMember] public bool PreloadDiFuMo256 { get; set; }
        [DataMember] public bool PreloadDiFuMo512 { get; set; }
        [DataMember] public bool PreloadDiFuMo1024 { get; set; }
        #endregion

        #region Constructors
        public AtlasesPreferences() : this(true, true, false, false, false, false, false, false)
        {

        }
        public AtlasesPreferences(bool preloadMarsAtlas, bool preloadJuBrain, bool preloadIBC, bool preloadDiFuMo64, bool preloadDiFuMo128, bool preloadDiFuMo256, bool preloadDiFuMo512, bool preloadDiFuMo1024)
        {
            PreloadMarsAtlas = preloadMarsAtlas;
            PreloadJuBrain = preloadJuBrain;
            PreloadIBC = preloadIBC;
            PreloadDiFuMo64 = preloadDiFuMo64;
            PreloadDiFuMo128 = preloadDiFuMo128;
            PreloadDiFuMo256 = preloadDiFuMo256;
            PreloadDiFuMo512 = preloadDiFuMo512;
            PreloadDiFuMo1024 = preloadDiFuMo1024;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new AtlasesPreferences(PreloadMarsAtlas, PreloadJuBrain, PreloadIBC, PreloadDiFuMo64, PreloadDiFuMo128, PreloadDiFuMo256, PreloadDiFuMo512, PreloadDiFuMo1024);
        }
        #endregion
    }
}