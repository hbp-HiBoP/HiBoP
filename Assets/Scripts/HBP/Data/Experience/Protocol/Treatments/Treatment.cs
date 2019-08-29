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
    public class Treatment : ICloneable, ICopiable, IIdentifiable
    {
        #region Properties
        [DataMember] public string ID { get; set; }
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
        public Treatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string id)
        {
            UseOnWindow = useOnWindow;
            Window = window;

            UseOnBaseline = useOnBaseline;
            Baseline = baseline;

            Order = order;
            ID = id;
        }
        #endregion

        #region Public Methods
        public virtual void Apply(ref float[] values, ref float[] baseline, int mainEventIndex, Frequency frequency)
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Treatment treatment = obj as Treatment;
            return (treatment != null && treatment.ID == ID);
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return this.ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First object to compare.</param>
        /// <param name="b">Second object to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Treatment a, Treatment b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        /// <summary>
        /// Operator not equals.
        /// </summary>
        /// <param name="a">First object to compare.</param>
        /// <param name="b">Second object to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Treatment a, Treatment b)
        {
            return !(a == b);
        }
        public virtual object Clone()
        {
            return new Treatment(UseOnWindow, Window, UseOnBaseline, Baseline, Order, ID);
        }
        public virtual void Copy(object copy)
        {
            Treatment treatment = copy as Treatment;
            UseOnWindow = treatment.UseOnWindow;
            Window = treatment.Window;
            UseOnBaseline = treatment.UseOnBaseline;
            Baseline = treatment.Baseline;
            Order = treatment.Order;
            ID = treatment.ID;
        }
        public virtual void GenerateID()
        {
            ID = Guid.NewGuid().ToString();
        }
        #endregion
    }
}