using System;
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
    public class SubBloc : ICloneable, ICopiable
    {
        #region Properties
        /// <summary>
        /// ID of the SubBloc.
        /// </summary>
        [DataMember] public string ID { get; set; }
        /// <summary>
        /// Name of the SubBloc.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Position of the SubBloc in the Bloc.
        /// </summary>
        [DataMember] public int Position { get; set; }
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
        public Event MainEvent { get { return Events.FirstOrDefault((e) => e.Type == Event.TypeEnum.Main); } }
        /// <summary>
        /// Secondary events of the SubBloc.
        /// </summary>
        public ReadOnlyCollection<Event> SecondaryEvents { get { return new ReadOnlyCollection<Event>(Events.FindAll((e) => e.Type == Event.TypeEnum.Secondary)); } }
        /// <summary>
        /// Events of the SubBloc.
        /// </summary>
        [DataMember] public List<Event> Events { get; set; }
        /// <summary>
        /// Iconic scenario of the SubBloc.
        /// </summary>
        [DataMember] public Scenario Scenario { get; set; }
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
        public SubBloc(string name, int position, Window window, Window baseline, IEnumerable<Event> events, Scenario scenario, IEnumerable<Treatment> treatments, string id)
        {
            ID = id;
            Name = name;
            Position = position;
            Window = window;
            Baseline = baseline;
            Events = events.ToList();
            Scenario = scenario;
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
        public SubBloc(string name, int position, Window window, Window baseline, IEnumerable<Event> events, Scenario scenario, IEnumerable<Treatment> treatments) : this(name, position, window, baseline, events, scenario, treatments, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new SubBloc with default value.
        /// </summary>
        public SubBloc() : this(string.Empty, 0, new Window(), new Window(), new List<Event>(), new Scenario(), new List<Treatment>())
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        public void Copy(object copy)
        {
            SubBloc subBloc = copy as SubBloc;
            ID = subBloc.ID;
            Name = subBloc.Name;
            Position = subBloc.Position;
            Window = subBloc.Window;
            Baseline = subBloc.Baseline;
            Events = subBloc.Events;
            Scenario = subBloc.Scenario;
            Treatments = subBloc.Treatments;
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new SubBloc(Name, Position, Window, Baseline, Events.ToArray().DeepClone(), Scenario.Clone() as Scenario, Treatments.ToArray().DeepClone(), ID.Clone() as string);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            SubBloc p = obj as SubBloc;
            if (p == null)
            {
                return false;
            }
            else
            {
                return ID == p.ID;
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First bloc to compare.</param>
        /// <param name="b">Second bloc to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(SubBloc a, SubBloc b)
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
        /// <param name="a">First bloc to compare.</param>
        /// <param name="b">Second bloc to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(SubBloc a, SubBloc b)
        {
            return !(a == b);
        }
        #endregion
    }
}