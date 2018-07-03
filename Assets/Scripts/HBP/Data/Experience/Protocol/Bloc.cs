using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.Unity;

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
        [DataMember] public string ID { get; set; }
        /// <summary>
        /// Name of the bloc.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Position of the bloc.
        /// </summary>
        [DataMember] public Position Position { get; set; }
        [DataMember(Name = "IllustrationPath")] string m_IllustrationPath = "";
        /// <summary>
        /// illustration path.
        /// </summary>
        [IgnoreDataMember] public string IllustrationPath
        {
            get
            {
                return m_IllustrationPath.ConvertToFullPath();
            }
            set
            {
                m_IllustrationPath = value.ConvertToShortPath();
            }
        }
        /// <summary>
        /// Sort lines of the bloc.
        /// </summary>
        [DataMember] public string Sort { get; set; }
        /// <summary>
        /// Window of the bloc (\a x : time before main event in ms. \a y : time after main event in ms.)
        /// </summary>
        [DataMember] public Window Window { get; set; }
        /// <summary>
        /// Baseline of the bloc (\a x : start of the Baseline in ms. \a y : end of the Baseline in ms.)
        /// </summary>
        [DataMember] public Window Baseline { get; set; }
        /// <summary>
        /// Main event of the bloc.
        /// </summary>
        public Event MainEvent { get { return Events.FirstOrDefault((e) => e.Type == Event.TypeEnum.Main); } }
        /// <summary>
        /// Secondary events of the bloc.
        /// </summary>
        public ReadOnlyCollection<Event> SecondaryEvents { get { return new ReadOnlyCollection<Event>(Events.FindAll((e) => e.Type == Event.TypeEnum.Secondary)); } }
        /// <summary>
        /// Events of the bloc.
        /// </summary>
        [DataMember] public List<Event> Events { get; set; }
        /// <summary>
        /// Iconic scenario of the bloc.
        /// </summary>
        [DataMember] public Scenario Scenario { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="secondaryEvents">Events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        /// <param name="id">Unique ID of the bloc.</param>
        public Bloc(string name, Position position, string illustrationPath, string sort, Window window, Window baseline, List<Event> events, Scenario scenario, string id)
        {
            ID = id;
            Name = name;
            Position = position;
            IllustrationPath = illustrationPath;
            Sort = sort;
            Window = window;
            Baseline = baseline;
            Events = events;
            Scenario = scenario;
        }
        /// <summary>
        /// Create a new bloc instance with a unique ID.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="events">Events of the bloc.</param>
        /// <param name="scenario">Iconic scenario of the bloc.</param>
        public Bloc(string name, Position position, string illustrationPath, string sort, Window window, Window Baseline, List<Event> events, Scenario scenario) : this(name,position,illustrationPath,sort,window,Baseline, events, scenario,Guid.NewGuid().ToString())
        {
        }
        public Bloc(Position position) : this ("New bloc",position,"","",new Window(),new Window(),new List<Event>(),new Scenario())
        {

        }
        /// <summary>
        /// Create a new bloc instance with default values.
        /// </summary>
        public Bloc() : this(string.Empty, new Position(), string.Empty, string.Empty, new Window(), new Window(), new List<Event>(), new Scenario())
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
            Bloc bloc = copy as Bloc;
            ID = bloc.ID;
            Name = bloc.Name;
            Position = bloc.Position;
            IllustrationPath = bloc.IllustrationPath;
            Sort = bloc.Sort;
            Window = bloc.Window;
            Baseline = bloc.Baseline;
            Events = bloc.Events;
            Scenario = bloc.Scenario;
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Bloc(Name, Position, m_IllustrationPath, Sort, Window, Baseline, Events.ToArray().DeepClone().ToList() , Scenario.Clone() as Scenario, ID.Clone() as string);
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