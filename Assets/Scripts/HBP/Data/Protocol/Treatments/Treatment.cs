using System.Runtime.Serialization;
using Tools.CSharp;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which define a treatment to apply at a subBloc.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Order</b></term> 
    /// <description>Order of the treatment.</description>
    /// </item>
    /// <item>
    /// <term><b>UseOnWindow</b></term> 
    /// <description>True if we apply the treatment on the window, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Window</b></term> 
    /// <description>Temporal window to apply the treatment on the window of the subBloc.</description>
    /// </item>
    /// <item>
    /// <term><b>UseOnBaseline</b></term> 
    /// <description>True if we apply the treatment on the baseline, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Baseline</b></term> 
    /// <description>Temporal window to apply the treatment on the baseline of the subBloc</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Treatment : BaseData
    {
        #region Properties
        /// <summary>
        /// Order of the tretmeant to apply to the subBloc.
        /// </summary>
        [DataMember] public int Order { get; set; }
        /// <summary>
        /// True if we apply the treatment on the window, False otherwise.
        /// </summary>
        [DataMember] public bool UseOnWindow { get; set; }
        /// <summary>
        /// Temporal window to apply the treatment on the window of the subBloc.
        /// </summary>
        [DataMember] public Window Window { get; set; }
        /// <summary>
        /// True if we apply the treatment on the baseline, False otherwise.
        /// </summary>
        [DataMember] public bool UseOnBaseline { get; set; }
        /// <summary>
        /// Temporal window to apply the treatment on the baseline of the subBloc.
        /// </summary>
        [DataMember] public Window Baseline { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Treatment instance with default values.
        /// </summary>
        public Treatment() : this(true, new Window(), false, new Window(), 0)
        {
        }
        /// <summary>
        /// Create a new Treatment instance.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public Treatment(string ID) : this(true, new Window(), false, new Window(), 0, ID)
        {

        }
        /// <summary>
        /// Create a new treatment window.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the treatmeants to apply to the subBloc</param>
        public Treatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order) : base()
        {
            UseOnWindow = useOnWindow;
            Window = window;
            UseOnBaseline = useOnBaseline;
            Baseline = baseline;
            Order = order;
        }
        /// <summary>
        /// Create a new treatment window.
        /// </summary>
        /// <param name="useOnWindow">True if we apply the treatment on the window, False otherwise</param>
        /// <param name="window">Temporal window to apply the treatment on the window of the subBloc</param>
        /// <param name="useOnBaseline">True if we apply the treatment on the baseline, False otherwise</param>
        /// <param name="baseline">Temporal window to apply the treatment on the baseline of the subBloc</param>
        /// <param name="order">Order of the tretmeants to apply to the subBloc</param>
        /// <param name="ID">Unique identifier</param>
        public Treatment(bool useOnWindow, Window window, bool useOnBaseline, Window baseline, int order, string ID) : base(ID)
        {
            UseOnWindow = useOnWindow;
            Window = window;
            UseOnBaseline = useOnBaseline;
            Baseline = baseline;
            Order = order;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Apply the treatment of the subBloc.
        /// </summary>
        /// <param name="values">Values in the window</param>
        /// <param name="baseline">Values in the baseline</param>
        /// <param name="windowMainEventIndex">window main event index in the window</param>
        /// <param name="baselineMainEventIndex">baseline main event index in the baseline</param>
        /// <param name="frequency">Frequency of the data</param>
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