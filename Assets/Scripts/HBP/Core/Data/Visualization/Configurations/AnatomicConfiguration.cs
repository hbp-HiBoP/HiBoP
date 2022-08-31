using System.Runtime.Serialization;

namespace HBP.Core.Data
{
    [DataContract]
    public class AnatomicConfiguration : BaseData
    {
        #region Properties
        #endregion

        #region Constructors
        public AnatomicConfiguration() : base()
        {

        }
        public AnatomicConfiguration(string ID) : base(ID)
        {

        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new AnatomicConfiguration(ID);
        }
        #endregion

        #region Private Methods
        #endregion
    }
}