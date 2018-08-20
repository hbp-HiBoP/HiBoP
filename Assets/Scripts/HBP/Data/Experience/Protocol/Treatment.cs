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
    public class Treatment : ICloneable, ICopiable
    {
        #region Properties
        #endregion

        #region Constructors
        public Treatment()
        {

        }
        #endregion

        #region Operators
        public object Clone()
        {
            return this;
        }
        public void Copy(object copy)
        {

        }
        #endregion
    }
}