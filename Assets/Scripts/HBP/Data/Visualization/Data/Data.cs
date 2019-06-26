using System.Collections.Generic;

namespace HBP.Data.Visualization
{
    public abstract class Data
    {
        #region Properties
        public virtual Dictionary<string, string> UnitByChannel { get; set; } = new Dictionary<string, string>();
        #endregion

        public virtual void Unload()
        {
            UnitByChannel.Clear();
        }
    }
}