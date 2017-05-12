using System;
using System.Linq;
using System.Collections.Generic;
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
	public class Bloc : ICloneable, ICopiable
	{
        #region Properties
        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Display informations of the bloc.
        /// </summary>
        public DisplayInformations DisplayInformations { get; set; }
        /// <summary>
        /// Main event of the bloc.
        /// </summary>
        public Event MainEvent { get; set; }
        /// <summary>
        /// Secondary events of the bloc.
        /// </summary>
        public List<Event> SecondaryEvents { get; set; }
        /// <summary>
        /// Iconic scenario of the bloc.
        /// </summary>
        public Scenario Scenario { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="mainEvent">Main event of the bloc.</param>
        /// <param name="secondaryEvents">Secondary events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        /// <param name="id">Unique ID of the bloc.</param>
        public Bloc(DisplayInformations displayInformations, Event mainEvent, List<Event> secondaryEvents, Scenario scenario, string id)
        {
            DisplayInformations = displayInformations;
            MainEvent = mainEvent;
            SecondaryEvents = secondaryEvents;
            Scenario = scenario;
            ID = id;
        }
        /// <summary>
        /// Create a new bloc instance with a unique ID.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="mainEvent">Main event of the bloc.</param>
        /// <param name="secondaryEvents">Secondary events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        public Bloc(DisplayInformations displayInformations, Event mainEvent, List<Event> secondaryEvents, Scenario scenario) : this(displayInformations, mainEvent, secondaryEvents, scenario,Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new bloc instance with display informations and default other values.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="mainEvent">Main event of the bloc.</param>
        /// <param name="secondaryEvents">Secondary events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        public Bloc(DisplayInformations displayInformations) : this(displayInformations, new Event(), new List<Event>(), new Scenario())
        {
        }
        /// <summary>
        /// Create a new bloc instance with default values.
        /// </summary>
        public Bloc() : this(new DisplayInformations(),new Event(), new List<Event>(), new Scenario())
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
            MainEvent = protocol.MainEvent;
            SecondaryEvents = protocol.SecondaryEvents;
            Scenario = protocol.Scenario;
            ID = protocol.ID;
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Bloc(DisplayInformations.Clone() as DisplayInformations, MainEvent.Clone() as Event, SecondaryEvents.ToArray().DeepClone().ToList() , Scenario.Clone() as Scenario, ID.Clone() as string);
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
                return DisplayInformations == p.DisplayInformations && MainEvent == p.MainEvent && Scenario == p.Scenario && System.Linq.Enumerable.SequenceEqual(SecondaryEvents, p.SecondaryEvents); 
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