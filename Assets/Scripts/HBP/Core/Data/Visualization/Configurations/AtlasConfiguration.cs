using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class AtlasConfiguration : BaseData
    {
        #region Properties

        [JsonProperty] public bool MarsAtlas { get; set; }
        [JsonProperty] public bool JuBrain { get; set; }
        [JsonProperty] public float AtlasAlpha { get; set; }
        [JsonProperty] public bool IBC { get; set; }
        [JsonProperty] public int IBCIndex { get; set; }
        [JsonProperty] public bool DiFuMo { get; set; }
        [JsonProperty] public string DiFuMoAtlas { get; set; }
        [JsonProperty] public int DiFuMoArea { get; set; }
        [JsonProperty] public bool Localizers { get; set; }
        [JsonProperty] public string LocalizerProtocol { get; set; }
        [JsonProperty] public string LocalizerData { get; set; }
        [JsonProperty] public string LocalizerBloc { get; set; }
        [JsonProperty] public int LocalizerTime { get; set; }
        [JsonProperty] public float FMRIAlpha { get; set; }
        [JsonProperty] public float NegativeMin { get; set; }
        [JsonProperty] public float NegativeMax { get; set; }
        [JsonProperty] public float PositiveMin { get; set; }
        [JsonProperty] public float PositiveMax { get; set; }
        [JsonProperty] public float LocalizerMin { get; set; }
        [JsonProperty] public float LocalizerMiddle { get; set; }
        [JsonProperty] public float LocalizerMax { get; set; }

        #endregion

        #region Constructors

        public AtlasConfiguration(bool marsAtlas, bool juBrain, float atlasAlpha, bool iBC, int iBCIndex, bool diFuMo, string diFuMoAtlas, int diFuMoArea, bool localizers, string localizerProtocol, string localizerData, string localizerBloc, int localizerTime, float fMRIAlpha, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float localizerMin, float localizerMiddle, float localizerMax) : this(marsAtlas, juBrain, atlasAlpha, iBC, iBCIndex, diFuMo, diFuMoAtlas, diFuMoArea, localizers, localizerProtocol, localizerData, localizerBloc, localizerTime, fMRIAlpha, negativeMin, negativeMax, positiveMin, positiveMax, localizerMin, localizerMiddle, localizerMax, null)
        {
        }

        public AtlasConfiguration(bool marsAtlas, bool juBrain, float atlasAlpha, bool iBC, int iBCIndex, bool diFuMo, string diFuMoAtlas, int diFuMoArea, bool localizers, string localizerProtocol, string localizerData, string localizerBloc, int localizerTime, float fMRIAlpha, float negativeMin, float negativeMax, float positiveMin, float positiveMax, float localizerMin, float localizerMiddle, float localizerMax, string ID) : base(ID)
        {
            MarsAtlas = marsAtlas;
            JuBrain = juBrain;
            AtlasAlpha = atlasAlpha;
            IBC = iBC;
            IBCIndex = iBCIndex;
            DiFuMo = diFuMo;
            DiFuMoAtlas = diFuMoAtlas;
            DiFuMoArea = diFuMoArea;
            Localizers = localizers;
            LocalizerProtocol = localizerProtocol;
            LocalizerData = localizerData;
            LocalizerBloc = localizerBloc;
            LocalizerTime = localizerTime;
            FMRIAlpha = fMRIAlpha;
            NegativeMin = negativeMin;
            NegativeMax = negativeMax;
            PositiveMin = positiveMin;
            PositiveMax = positiveMax;
            LocalizerMin = localizerMin;
            LocalizerMiddle = localizerMiddle;
            LocalizerMax = localizerMax;
        }

        public AtlasConfiguration() : this(false, false, 1f, false, 0, false, null, 0, false, null, null, null, 0, .2f, .05f, .5f, .05f, .5f, 80f, 100f, 120f)
        {
        }

        #endregion

        #region Public Methods

        public override object Clone()
        {
            return new AtlasConfiguration(MarsAtlas, JuBrain, AtlasAlpha, IBC, IBCIndex, DiFuMo, DiFuMoAtlas, DiFuMoArea, Localizers, LocalizerProtocol, LocalizerData, LocalizerBloc, LocalizerTime, FMRIAlpha, NegativeMin, NegativeMax, PositiveMin, PositiveMax, LocalizerMin, LocalizerMiddle, LocalizerMax, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AtlasConfiguration configuration)
            {
                MarsAtlas = configuration.MarsAtlas;
                JuBrain = configuration.JuBrain;
                AtlasAlpha = configuration.AtlasAlpha;
                IBC = configuration.IBC;
                IBCIndex = configuration.IBCIndex;
                DiFuMo = configuration.DiFuMo;
                DiFuMoAtlas = configuration.DiFuMoAtlas;
                DiFuMoArea = configuration.DiFuMoArea;
                Localizers = configuration.Localizers;
                LocalizerProtocol = configuration.LocalizerProtocol;
                LocalizerData = configuration.LocalizerData;
                LocalizerBloc = configuration.LocalizerBloc;
                LocalizerTime = configuration.LocalizerTime;
                FMRIAlpha = configuration.FMRIAlpha;
                NegativeMin = configuration.NegativeMin;
                NegativeMax = configuration.NegativeMax;
                PositiveMin = configuration.PositiveMin;
                PositiveMax = configuration.PositiveMax;
                LocalizerMin = configuration.LocalizerMin;
                LocalizerMiddle = configuration.LocalizerMiddle;
                LocalizerMax = configuration.LocalizerMax;
            }
        }

        #endregion
    }
}
