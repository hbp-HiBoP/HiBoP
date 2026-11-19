namespace HBP.Core.Data
{
    using HBP.Core.Tools;
    using Newtonsoft.Json;

    public class AnatomicConfiguration : BaseData
    {
        #region Properties

        [JsonProperty] public float MaximumInfluence { get; set; } = 15f;
        [JsonProperty("Span Min Ratio")] public float SpanMinRatio { get; set; } = 0.05f;
        [JsonProperty("Middle Ratio")] public float MiddleRatio { get; set; } = 0.35f;
        [JsonProperty("Span Max Ratio")] public float SpanMaxRatio { get; set; } = 0.65f;

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            ThresholdRatioUtility.Validate(SpanMinRatio, MiddleRatio, SpanMaxRatio);
        }

        protected override void OnSerializing()
        {
            base.OnSerializing();
            ThresholdRatioUtility.Validate(SpanMinRatio, MiddleRatio, SpanMaxRatio);
        }

        #endregion

        #region Constructors

        public AnatomicConfiguration() : this(15f, 0.05f, 0.35f, 0.65f)
        {
        }

        public AnatomicConfiguration(string ID) : this(15f, 0.05f, 0.35f, 0.65f, ID)
        {
        }

        public AnatomicConfiguration(float maximumInfluence, float spanMinRatio, float middleRatio, float spanMaxRatio) : this(maximumInfluence, spanMinRatio, middleRatio, spanMaxRatio, null)
        {
        }

        public AnatomicConfiguration(float maximumInfluence, float spanMinRatio, float middleRatio, float spanMaxRatio, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
            SpanMinRatio = spanMinRatio;
            MiddleRatio = middleRatio;
            SpanMaxRatio = spanMaxRatio;
        }

        #endregion

        #region Public Methods

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AnatomicConfiguration configuration)
            {
                MaximumInfluence = configuration.MaximumInfluence;
                SpanMinRatio = configuration.SpanMinRatio;
                MiddleRatio = configuration.MiddleRatio;
                SpanMaxRatio = configuration.SpanMaxRatio;
            }
        }

        public override object Clone()
        {
            return new AnatomicConfiguration(MaximumInfluence, SpanMinRatio, MiddleRatio, SpanMaxRatio, ID);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
