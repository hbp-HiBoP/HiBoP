using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tools.CSharp;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which define a bloc in a visualisation protocol.
    ///     - Display bloc.
    ///     - Main event.
    ///     - Secondary events.
    ///     - Scenario.
    ///     - ID.
    /// </summary>
    [Serializable]
	public class Bloc : ICloneable, ICopiable
	{
        #region Properties
        [SerializeField]
        private string id;
        public string ID
        {
            get { return id; }
            private set { id = value; }
        }

        [SerializeField]
        private DisplayInformations displayInformations;
        public DisplayInformations DisplayInformations
        {
            get {return displayInformations; }
            set { displayInformations = value; }
        }
		
        [SerializeField]
		private Event mainEvent;
        public Event MainEvent
        {
            get { return mainEvent; }
            set { mainEvent = value; }
        }

        [SerializeField]
        private List<Event> secondaryEvents;
        public ReadOnlyCollection<Event> SecondaryEvents
        {
            get { return new ReadOnlyCollection<Event>(secondaryEvents); }
        }

        [SerializeField]
        private Scenario scenario;
        public Scenario Scenario
        {
            get { return scenario; }
            set { scenario = value; }
        }
        #endregion

        #region Constructors
        public Bloc(DisplayInformations displayInformations, Event mainEvent, List<Event> secondaryEvents, Scenario scenario, string id)
        {
            DisplayInformations = displayInformations;
            MainEvent = mainEvent;
            this.secondaryEvents = secondaryEvents;
            Scenario = scenario;
            ID = id;
        }
        public Bloc(DisplayInformations displayInformations, Event mainEvent, List<Event> secondaryEvents, Scenario scenario) : this(displayInformations, mainEvent, secondaryEvents, scenario,Guid.NewGuid().ToString())
        {
        }
        public Bloc(DisplayInformations displayInformations) : this(displayInformations, new Event(), new List<Event>(), new Scenario())
        {
        }
        public Bloc() : this(new DisplayInformations(),new Event(), new List<Event>(), new Scenario())
		{
		}
        #endregion

        #region Public Methods
        public void SetSecondaryEvents(Event[] secondaryEvents)
        {
            this.secondaryEvents = new List<Event>(secondaryEvents);
        }
        public void AddSecondaryEvent(Event secondaryEvent)
        {
            secondaryEvents.Add(secondaryEvent);
        }
        public void AddSecondaryEvents(Event[] secondaryEvents)
        {
            foreach(Event secondaryEvent in secondaryEvents)
            {
                AddSecondaryEvent(secondaryEvent);
            }
        }
        public void RemoveSecondaryEvent(Event secondaryEvent)
        {
            secondaryEvents.Remove(secondaryEvent);
        }
        public void RemoveSecondaryEvent(Event[] secondaryEvents)
        {
            foreach (Event secondaryEvent in secondaryEvents)
            {
                RemoveSecondaryEvent(secondaryEvent);
            }
        }
        public void ClearSecondaryEvents()
        {
            secondaryEvents = new List<Event>();
        }
        #endregion

        #region Operators
        public void Copy(object copy)
        {
            Bloc protocol = copy as Bloc;
            DisplayInformations = protocol.DisplayInformations;
            MainEvent = protocol.MainEvent;
            SetSecondaryEvents(protocol.SecondaryEvents.ToArray());
            Scenario = protocol.Scenario;
            ID = protocol.ID;
        }
        public object Clone()
        {
            return new Bloc(DisplayInformations.Clone() as DisplayInformations, MainEvent.Clone() as Event, SecondaryEvents.ToArray().DeepClone().ToList() , Scenario.Clone() as Scenario, ID.Clone() as string);
        }
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
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
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
        public static bool operator !=(Bloc a, Bloc b)
        {
            return !(a == b);
        }
        #endregion
    }
}
