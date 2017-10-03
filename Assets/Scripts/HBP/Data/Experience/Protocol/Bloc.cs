using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Bloc
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 janvier 2017
    * \brief Bloc in a Protocol.
    * 
    * \details Class which define a bloc in a visualization protocol which contains : 
    *     - Display informations.
    *     - Main event.
    *     - Secondary events.
    *     - Iconic scenario.
    *     - Unique ID.
    */
    [DataContract]
    public class Bloc : ICloneable, ICopiable
	{
        #region Properties
        /// <summary>
        /// Unique ID.
        /// </summary>
        [DataMember]
        public string ID { get; set; }
        /// <summary>
        /// Display informations of the bloc.
        /// </summary>
        [DataMember]
        public DisplayInformations DisplayInformations { get; set; }
        /// <summary>
        /// Main event of the bloc.
        /// </summary>
        public Event MainEvent { get { return Events.First((e) => e.Type == Event.TypeEnum.Main); } }
        /// <summary>
        /// Secondary events of the bloc.
        /// </summary>
        public ReadOnlyCollection<Event> SecondaryEvents { get { return new ReadOnlyCollection<Event>(Events.FindAll((e) => e.Type == Event.TypeEnum.Secondary)); } }
        /// <summary>
        /// Events of the bloc.
        /// </summary>
        [DataMember]
        public List<Event> Events { get; set; }
        /// <summary>
        /// Iconic scenario of the bloc.
        /// </summary>
        [DataMember]
        public Scenario Scenario { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="secondaryEvents">Events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        /// <param name="id">Unique ID of the bloc.</param>
        public Bloc(DisplayInformations displayInformations, List<Event> events, Scenario scenario, string id)
        {
            DisplayInformations = displayInformations;
            Events = events;
            Scenario = scenario;
            ID = id;
        }
        /// <summary>
        /// Create a new bloc instance with a unique ID.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="events">Events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        public Bloc(DisplayInformations displayInformations, List<Event> events, Scenario scenario) : this(displayInformations, events, scenario,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new bloc instance with display informations and default other values.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        public Bloc(DisplayInformations displayInformations) : this(displayInformations, new List<Event>(), new Scenario())
        {
        }
        /// <summary>
        /// Create a new bloc instance with default values.
        /// </summary>
        public Bloc() : this(new DisplayInformations())
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
            Bloc protocol = copy as Bloc;
            DisplayInformations = protocol.DisplayInformations;
            Events = protocol.Events;
            Scenario = protocol.Scenario;
            ID = protocol.ID;
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Bloc(DisplayInformations.Clone() as DisplayInformations, Events.ToArray().DeepClone().ToList() , Scenario.Clone() as Scenario, ID.Clone() as string);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Bloc p = obj as Bloc;
            if (p == null)
            {
                return false;
            }
            else
            {
                return DisplayInformations == p.DisplayInformations && Scenario == p.Scenario && Enumerable.SequenceEqual(Events, p.Events); 
            }
        }
        /// <summary>
        /// Get hash code.
        /// </summary>
        /// <returns>HashCode.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Operator equals.
        /// </summary>
        /// <param name="a">First bloc to compare.</param>
        /// <param name="b">Second bloc to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Bloc a, Bloc b)
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
        public static bool operator !=(Bloc a, Bloc b)
        {
            return !(a == b);
        }
        #endregion
    }
}