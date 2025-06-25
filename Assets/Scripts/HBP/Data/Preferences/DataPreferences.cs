using System;
using HBP.Core.Data;
using HBP.Core.Enums;
using Newtonsoft.Json;

namespace HBP.Data.Preferences
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DataPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public EEGPreferences EEG { get; set; }
        [JsonProperty] public ProtocolPreferences Protocol { get; set; }
        [JsonProperty] public AnatomicPreferences Anatomic { get; set; }
        [JsonProperty] public AtlasesPreferences Atlases { get; set; }
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

    public class EEGPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public AveragingType Averaging
        {
            get
            {
                return DataManager.DefaultAveraging;
            }
            set
            {
                DataManager.DefaultAveraging = value;
            }
        }
        [JsonProperty] public NormalizationType Normalization
        {
            get
            {
                return DataManager.DefaultNormalization;
            }
            set
            {
                DataManager.DefaultNormalization = value;
            }
        }
        [JsonProperty] public float CorrelationAlpha { get; set; }
        [JsonProperty] public bool BonferroniCorrection { get; set; }
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

    [JsonObject(MemberSerialization.OptIn)]
    public class ProtocolPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public AveragingType PositionAveraging
        {
            get
            {
                return DataManager.DefaultPositionAveraging;
            }
            set
            {
                DataManager.DefaultPositionAveraging = value;
            }
        }
        [JsonProperty] public float MinLimit { get; set; }
        [JsonProperty] public float MaxLimit { get; set; }
        [JsonProperty] public int Step { get; set; }
        #endregion

        #region Constructors
        public ProtocolPreferences() : this(AveragingType.Median, -10000, 10000, 100)
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

    [JsonObject(MemberSerialization.OptIn)]
    public class AnatomicPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool SiteNameCorrection
        {
            get
            {
                return Site.SiteNameCorrection;
            }
            set
            {
                Site.SiteNameCorrection = value;
            }
        }
        [JsonProperty] public bool MeshPreloading { get; set; }
        [JsonProperty] public bool MRIPreloading { get; set; }
        [JsonProperty] public bool ImplantationPreloading { get; set; }
        [JsonProperty] public bool PreloadSinglePatientDataInMultiPatientVisualization { get; set; }
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

    [JsonObject(MemberSerialization.OptIn)]
    public class AtlasesPreferences : ICloneable
    {
        #region Properties
        [JsonProperty] public bool PreloadMarsAtlas { get; set; }
        [JsonProperty] public bool PreloadJuBrain { get; set; }
        [JsonProperty] public bool PreloadIBC { get; set; }
        [JsonProperty] public bool PreloadDiFuMo64 { get; set; }
        [JsonProperty] public bool PreloadDiFuMo128 { get; set; }
        [JsonProperty] public bool PreloadDiFuMo256 { get; set; }
        [JsonProperty] public bool PreloadDiFuMo512 { get; set; }
        [JsonProperty] public bool PreloadDiFuMo1024 { get; set; }
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