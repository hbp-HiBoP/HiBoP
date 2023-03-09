using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public class StaticConfiguration : BaseData
    {
        #region Properties
        #endregion

        #region Constructors
        public StaticConfiguration() : base()
        {
        }
        public StaticConfiguration(string ID) : base(ID)
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new StaticConfiguration(ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is StaticConfiguration staticConfiguration)
            {
            }
        }
        #endregion

        #region Private Methods
        #endregion
    }
}