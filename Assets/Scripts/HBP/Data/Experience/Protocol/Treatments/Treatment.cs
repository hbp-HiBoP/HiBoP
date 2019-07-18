using System;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.CSharp.EEG;

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
        [DataMember] public int Order { get; set; }
        [DataMember] public Window Window { get; set; }
        #endregion

        #region Constructors
        public Treatment() : this(new Window(), 0)
        {
        }
        public Treatment(Window window, int order)
        {
            Window = window;
            Order = order;
        }
        #endregion

        #region Public Methods
        public virtual float[] Apply(float[] values, int mainEventIndex, Frequency frequency)
        {
            return values;
        }
        #endregion

        #region Operators
        public virtual object Clone()
        {
            return new Treatment(Window, Order);
        }
        public void Copy(object copy)
        {
            Treatment treatment = copy as Treatment;
            Window = treatment.Window;
            Order = treatment.Order;
        }
        #endregion
    }
}