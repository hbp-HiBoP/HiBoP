namespace HBP.Data.Experience.Dataset
{
    public struct SiteSubTrial
    {
        #region Properties
        public float[] Values { get; set; }
        public bool Found { get; set; }
        #endregion

        #region Constructors
        public SiteSubTrial(float[] values, bool found) : this()
        {
            Values = values;
            Found = found;
        }
        public SiteSubTrial(SubTrial subTrial, string site)
        {
            Values = subTrial.ValuesByChannel[site];
            Found = subTrial.Found;
        }
        #endregion
    }
}