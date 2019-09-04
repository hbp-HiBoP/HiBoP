using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class SubBloc
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 juin 2018
    * \brief SubBloc in a Bloc.
    * 
    * \details Class which define a subBloc in a visualization protocol bloc which contains : 
    *     - Unique ID.
    *     - Name
    *     - Position.
    *     - Window.
    *     - Baseline.
    *     - Events.
    *     - Iconic Scenario.
    *     - Treatments.
    */
    [DataContract]
    public class SubBloc : BaseData
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
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new SubBloc.
        /// </summary>
        /// <param name="name">Name of the subBloc.</param>
        /// <param name="position">Position of the subBloc.</param>
        /// <param name="window">Window of the subBloc.</param>
        /// <param name="baseline">Baseline of the subBloc.</param>
        /// <param name="events">Events of the subBloc.</param>
        /// <param name="scenario">Iconic Scenario of the subBloc.</param>
        /// <param name="treatments">Treatments of the subBloc.</param>
        /// <param name="id">Unique ID of the subBloc.</param>
        public SubBloc(string name, int order, Enums.MainSecondaryEnum type, Window window, Window baseline, IEnumerable<Event> events, IEnumerable<Icon> icons, IEnumerable<Treatment> treatments, string id) : base(id)
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
        /// Create a new SubBloc with a uniqueID.
        /// </summary>
        /// <param name="name">Name of the subBloc.</param>
        /// <param name="position">Position of the subBloc.</param>
        /// <param name="window">Window of the subBloc.</param>
        /// <param name="baseline">Baseline of the subBloc.</param>
        /// <param name="events">Events of the subBloc.</param>
        /// <param name="scenario">Iconic Scenario of the subBloc.</param>
        /// <param name="treatments">Treatments of the subBloc.</param>
        /// <param name="id">Unique ID of the subBloc.</param>
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
        /// Create a new SubBloc with default value.
        /// </summary>
        public SubBloc() : this(string.Empty, 0, Enums.MainSecondaryEnum.Main, new Window(-300,300), new Window(-300,0), new List<Event>(), new List<Icon>(), new List<Treatment>())
        {
        }
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