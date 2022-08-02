namespace HBP.Core.Data.Processed
{
    public abstract class DynamicData : Data
    {
        #region Properties
        public virtual IconicScenario IconicScenario { get; set; }
        public virtual Timeline Timeline { get; set; }
        #endregion

        public override void Unload()
        {
            base.Unload();
            IconicScenario = null;
            Timeline = null;
        }
    }
}