namespace HBP.Core.Data
{
    public class AnatomicConfiguration : BaseData
    {
        #region Properties

        [Newtonsoft.Json.JsonProperty] public float MaximumInfluence { get; set; } = 15f;

        #endregion

        #region Constructors

        public AnatomicConfiguration() : this(15f)
        {
        }

        public AnatomicConfiguration(string ID) : this(15f, ID)
        {
        }

        public AnatomicConfiguration(float maximumInfluence) : this(maximumInfluence, null)
        {
        }

        public AnatomicConfiguration(float maximumInfluence, string ID) : base(ID)
        {
            MaximumInfluence = maximumInfluence;
        }

        #endregion

        #region Public Methods

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AnatomicConfiguration configuration) MaximumInfluence = configuration.MaximumInfluence;
        }

        public override object Clone()
        {
            return new AnatomicConfiguration(MaximumInfluence, ID);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
