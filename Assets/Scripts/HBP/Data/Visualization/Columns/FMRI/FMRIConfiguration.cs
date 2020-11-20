using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class FMRIConfiguration : BaseData
    {
        #region Properties
        #endregion

        #region Constructors
        public FMRIConfiguration() : base()
        {

        }
        public FMRIConfiguration(string ID) : base(ID)
        {

        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new FMRIConfiguration(ID);
        }
        #endregion

        #region Private Methods
        #endregion
    }
}