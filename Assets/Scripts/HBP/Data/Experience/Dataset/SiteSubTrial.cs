namespace HBP.Data.Experience.Dataset
{
    public struct SiteSubTrial
    {
        #region Properties
        public float[] Values { get; set; }
        #endregion

        #region Constructors
        public SiteSubTrial(float[] values) : this()
        {
            Values = values;
        }
        public SiteSubTrial(SubTrial subTrial, string site)
        {
            Values = subTrial.ValuesByChannel[site];
        }
        #endregion
    }
}