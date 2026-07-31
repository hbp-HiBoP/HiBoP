using HBP.Core.Data;
using HBP.Core.Enums;
using Newtonsoft.Json;
using System;
using UnityEngine.Scripting;

namespace HBP.Core.Preferences
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
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

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class EEGPreferences : ICloneable
    {
        #region Properties

        [JsonProperty] public AveragingType Averaging
        {
            get { return DataManager.DefaultAveraging; }
            set { DataManager.DefaultAveraging = value; }
        }

        [JsonProperty] public NormalizationType Normalization
        {
            get { return DataManager.DefaultNormalization; }
            set { DataManager.DefaultNormalization = value == NormalizationType.Auto ? NormalizationType.None : value; }
        }

        [JsonProperty] public TemporalSamplingPolicy TemporalSampling { get; set; }
        [JsonProperty] public float CorrelationAlpha { get; set; }
        [JsonProperty] public bool BonferroniCorrection { get; set; }

        #endregion

        #region Constructors

        public EEGPreferences() : this(AveragingType.Median, NormalizationType.None, 0.05f, true, TemporalSamplingPolicy.Interpolate)
        {
        }

        public EEGPreferences(AveragingType averaging, NormalizationType normalization, float correlationAlpha, bool bonferroniCorrection, TemporalSamplingPolicy temporalSampling = TemporalSamplingPolicy.Interpolate)
        {
            Averaging = averaging;
            Normalization = normalization;
            CorrelationAlpha = correlationAlpha;
            BonferroniCorrection = bonferroniCorrection;
            TemporalSampling = temporalSampling;
        }

        #endregion

        #region Public Methods

        public object Clone()
        {
            return new EEGPreferences(Averaging, Normalization, CorrelationAlpha, BonferroniCorrection, TemporalSampling);
        }

        #endregion
    }

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class ProtocolPreferences : ICloneable
    {
        #region Properties

        [JsonProperty] public AveragingType PositionAveraging
        {
            get { return DataManager.DefaultPositionAveraging; }
            set { DataManager.DefaultPositionAveraging = value; }
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

    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class AnatomicPreferences : ICloneable
    {
        #region Properties

        [JsonProperty] public bool SiteNameCorrection
        {
            get { return Site.SiteNameCorrection; }
            set { Site.SiteNameCorrection = value; }
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

    [JsonObject(MemberSerialization.OptIn), Preserve]
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
        [JsonProperty] public bool PreloadLocalizerAUDI { get; set; }
        [JsonProperty] public bool PreloadLocalizerLEC1 { get; set; }
        [JsonProperty] public bool PreloadLocalizerLEC2 { get; set; }
        [JsonProperty] public bool PreloadLocalizerMCSE { get; set; }
        [JsonProperty] public bool PreloadLocalizerMOTO { get; set; }
        [JsonProperty] public bool PreloadLocalizerMVEB { get; set; }
        [JsonProperty] public bool PreloadLocalizerMVIS { get; set; }
        [JsonProperty] public bool PreloadLocalizerVISU { get; set; }

        #endregion

        #region Constructors

        public AtlasesPreferences() : this(true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false)
        {
        }

        public AtlasesPreferences(bool preloadMarsAtlas, bool preloadJuBrain, bool preloadIBC, bool preloadDiFuMo64, bool preloadDiFuMo128, bool preloadDiFuMo256, bool preloadDiFuMo512, bool preloadDiFuMo1024, bool preloadLocalizerAUDI, bool preloadLocalizerLEC1, bool preloadLocalizerLEC2, bool preloadLocalizerMCSE, bool preloadLocalizerMOTO, bool preloadLocalizerMVEB, bool preloadLocalizerMVIS, bool preloadLocalizerVISU)
        {
            PreloadMarsAtlas = preloadMarsAtlas;
            PreloadJuBrain = preloadJuBrain;
            PreloadIBC = preloadIBC;
            PreloadDiFuMo64 = preloadDiFuMo64;
            PreloadDiFuMo128 = preloadDiFuMo128;
            PreloadDiFuMo256 = preloadDiFuMo256;
            PreloadDiFuMo512 = preloadDiFuMo512;
            PreloadDiFuMo1024 = preloadDiFuMo1024;
            PreloadLocalizerAUDI = preloadLocalizerAUDI;
            PreloadLocalizerLEC1 = preloadLocalizerLEC1;
            PreloadLocalizerLEC2 = preloadLocalizerLEC2;
            PreloadLocalizerMCSE = preloadLocalizerMCSE;
            PreloadLocalizerMOTO = preloadLocalizerMOTO;
            PreloadLocalizerMVEB = preloadLocalizerMVEB;
            PreloadLocalizerMVIS = preloadLocalizerMVIS;
            PreloadLocalizerVISU = preloadLocalizerVISU;
        }

        #endregion

        #region Public Methods

        public object Clone()
        {
            return new AtlasesPreferences(PreloadMarsAtlas, PreloadJuBrain, PreloadIBC, PreloadDiFuMo64, PreloadDiFuMo128, PreloadDiFuMo256, PreloadDiFuMo512, PreloadDiFuMo1024, PreloadLocalizerAUDI, PreloadLocalizerLEC1, PreloadLocalizerLEC2, PreloadLocalizerMCSE, PreloadLocalizerMOTO, PreloadLocalizerMVEB, PreloadLocalizerMVIS, PreloadLocalizerVISU);
        }

        #endregion
    }
}
