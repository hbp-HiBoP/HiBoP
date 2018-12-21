using System;
using System.Runtime.Serialization;

[DataContract]
public class AnantomicConfiguration : ICloneable
{
    #region Properties
    #endregion

    #region Constructors
    public AnantomicConfiguration()
    {

    }
    #endregion

    #region Public Methods
    public object Clone()
    {
        return new AnantomicConfiguration();
    }
    #endregion

    #region Private Methods
    #endregion
}