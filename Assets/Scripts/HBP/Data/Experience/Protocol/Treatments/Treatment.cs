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
    public class Treatment : BaseData
    {
        #region Properties
        [DataMember] public int Order { get; set; }
        [DataMember] public bool UseOnWindow { get; set; }
        [DataMember] public Window Window { get; set; }
        [DataMember] public bool UseOnBaseline { get; set; }
        [DataMember] public Window Baseline { get; set; }
        #endregion

        #region Constructors
        public Treatment() : this(true, new Window(), false, new Window(), 0, Guid.NewGuid().ToString())
        {
        }
        public Treatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id) : base(id)
        {
            UseOnWindow = useOnWindow;
            Window = window;
            UseOnBaseline = useOnBaseline;
            Baseline = baseline;
            Order = order;
        }
        public Treatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order) : base()
        {
            UseOnWindow = useOnWindow;
            Window = window;
            UseOnBaseline = useOnBaseline;
            Baseline = baseline;
            Order = order;
        }
        #endregion

        #region Public Methods
        public virtual void Apply(ref float[] values, ref float[] baseline, int windowMainEventIndex, int baselineMainEventIndex, Frequency frequency)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new Treatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is Treatment treatment)
            {
                UseOnWindow = treatment.UseOnWindow;
                Window = treatment.Window;
                UseOnBaseline = treatment.UseOnBaseline;
                Baseline = treatment.Baseline;
                Order = treatment.Order;
            }
        }
        #endregion
    }
}