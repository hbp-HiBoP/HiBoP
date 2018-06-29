using System;
using System.Runtime.Serialization;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Treatment
    * \author Adrien Gannerie
    * \version 1.0
    * \date 29 juin 2017
    * \brief SubBloc treatment.
    * 
    * \details Class which define a treatment to apply at a subBloc.
    */
    [DataContract]
    public abstract class Treatment : ICloneable, ICopiable
    {
        #region Operators
        public abstract object Clone();
        public abstract void Copy(object copy);
        #endregion
    }
}