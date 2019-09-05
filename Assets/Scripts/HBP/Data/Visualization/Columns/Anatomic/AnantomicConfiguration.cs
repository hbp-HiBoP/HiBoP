using System;
using System.Runtime.Serialization;

[DataContract]
public class AnatomicConfiguration : ICloneable
{
    #region Properties
    #endregion

    #region Constructors
    public AnatomicConfiguration()
    {

    }
    #endregion

    #region Public Methods
    public object Clone()
    {
        return new AnatomicConfiguration();
    }
    #endregion

    #region Private Methods
    #endregion
}