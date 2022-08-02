using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a experience subBloc used to epoch, and visualize data.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier</description>
    /// </item>
    /// <item>
    /// <term><b>Name</b></term> 
    /// <description>Name of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Order</b></term> 
    /// <description>Order of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Type</b></term> 
    /// <description>Type of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Window</b></term> 
    /// <description>Window of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Baseline</b></term> 
    /// <description>Baseline of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Events</b></term> 
    /// <description>Events of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Icons</b></term> 
    /// <description>Icons of the subBloc</description>
    /// </item>
    /// <item>
    /// <term><b>Treatments</b></term> 
    /// <description>Treatments of the subBloc</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class SubBloc : BaseData, INameable
    {
        #region Properties
        /// <summary> 
        /// Name of the SubBloc.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Order of the SubBloc in the Bloc.
        /// </summary>
        [DataMember] public int Order { get; set; }
        /// <summary>
        /// Type of SubBloc.
        /// </summary>
        [DataMember] public Enums.MainSecondaryEnum Type { get; set; }
        /// <summary>
        /// Window of the SubBloc (\a x : time before main event in ms. \a y : time after main event in ms.)
        /// </summary>
        [DataMember] public Window Window { get; set; }
        /// <summary>
        /// Baseline of the SubBloc (\a x : start of the Baseline in ms. \a y : end of the Baseline in ms.)
        /// </summary>
        [DataMember] public Window Baseline { get; set; }
        /// <summary>
        /// Main event of the SubBloc.
        /// </summary>
        public Event MainEvent { get { return Events.FirstOrDefault((e) => e.Type == Enums.MainSecondaryEnum.Main); } }
        /// <summary>
        /// Secondary events of the SubBloc.
        /// </summary>
        public ReadOnlyCollection<Event> SecondaryEvents { get { return new ReadOnlyCollection<Event>(Events.FindAll((e) => e.Type == Enums.MainSecondaryEnum.Secondary)); } }
        /// <summary>
        /// Events of the SubBloc.
        /// </summary>
        [DataMember] public List<Event> Events { get; set; }
        /// <summary>
        /// Iconic scenario of the SubBloc.
        /// </summary>
        [DataMember] public List<Icon> Icons { get; set; }
        /// <summary>
        /// Treatments of the subBloc.
        /// </summary>
        [DataMember] public List<Treatment> Treatments { get; set; }
        /// <summary>
        /// True if the subBloc is visualizable, False otherwise.
        /// </summary>
        public bool IsVisualizable
        {
            get
            {
                return Window.Lenght > 0 && MainEvent != null && Events.All(e => e.IsVisualizable);
            }
        }

        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var eve in Events) eve.GenerateID();
            foreach (var icon in Icons) icon.GenerateID();
            foreach (var treatment in Treatments) treatment.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var eve in Events) IDs.AddRange(eve.GetAllIdentifiable());
            foreach (var icon in Icons) IDs.AddRange(icon.GetAllIdentifiable());
            foreach (var treatment in Treatments) IDs.AddRange(treatment.GetAllIdentifiable());
            return IDs;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new SubBloc instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="order">Order</param>
        /// <param name="type">Type</param>
        /// <param name="window">Window</param>
        /// <param name="baseline">Baseline</param>
        /// <param name="events">Events</param>
        /// <param name="icons">Icons</param>
        /// <param name="treatments">Treatments</param>
        /// <param name="ID">Unique identifier</param>
        public SubBloc(string name, int order, Enums.MainSecondaryEnum type, Window window, Window baseline, IEnumerable<Event> events, IEnumerable<Icon> icons, IEnumerable<Treatment> treatments, string ID) : base(ID)
        {
            Name = name;
            Order = order;
            Type = type;
            Window = window;
            Baseline = baseline;
            Events = events.ToList();
            Icons = icons.ToList();
            Treatments = treatments.ToList();
        }
        /// <summary>
        /// Create a new SubBloc instance.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="order">Order</param>
        /// <param name="type">Type</param>
        /// <param name="window">Window</param>
        /// <param name="baseline">Baseline</param>
        /// <param name="events">Events</param>
        /// <param name="icons">Icons</param>
        /// <param name="treatments">Treatments</param>
        public SubBloc(string name, int order, Enums.MainSecondaryEnum type, Window window, Window baseline, IEnumerable<Event> events, IEnumerable<Icon> icons, IEnumerable<Treatment> treatments) : base()
        {
            Name = name;
            Order = order;
            Type = type;
            Window = window;
            Baseline = baseline;
            Events = events.ToList();
            Icons = icons.ToList();
            Treatments = treatments.ToList();
        }
        /// <summary>
        /// Create a new SubBloc instance with default value.
        /// </summary>
        public SubBloc() : this("New subBloc", 0, Enums.MainSecondaryEnum.Main, new Window(-300,300), new Window(-300,0), new List<Event>(), new List<Icon>(), new List<Treatment>())
        {
        }
        /// <summary>
        /// Create a new SubBloc instance with a specific type.
        /// </summary>
        /// <param name="type">Type</param>
        public SubBloc(Enums.MainSecondaryEnum type) : this()
        {
            Type = type;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if(obj is SubBloc subBloc)
            {
                Name = subBloc.Name;
                Order = subBloc.Order;
                Type = subBloc.Type;
                Window = subBloc.Window;
                Baseline = subBloc.Baseline;
                Events = subBloc.Events;
                Icons = subBloc.Icons;
                Treatments = subBloc.Treatments;
            }
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new SubBloc(Name, Order, Type, Window, Baseline, Events.DeepClone(), Icons.DeepClone(), Treatments.DeepClone(), ID);
        }
        #endregion
    }
}