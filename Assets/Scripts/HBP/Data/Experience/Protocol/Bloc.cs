using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Bloc
    * \author Adrien Gannerie
    * \version 2.0
    * \date 28 juin 2018
    * \brief Bloc in a Protocol.
    * 
    * \details Class which define a bloc in a visualization protocol which contains : 
    *     - Unique ID.
    *     - Name
    *     - Position.
    *     - Image.
    *     - Sorting.
    */
    [DataContract]
    public class Bloc : ICloneable, ICopiable
	{
        #region Properties
        /// <summary>
        /// Unique ID of the bloc.
        /// </summary>
        [DataMember] public string ID { get; set; }
        /// <summary>
        /// Name of the bloc.
        /// </summary>
        [DataMember] public string Name { get; set; }
        /// <summary>
        /// Position of the bloc.
        /// </summary>
        [DataMember] public int Position { get; set; }
        [DataMember(Name = "IllustrationPath")] string m_IllustrationPath = "";
        /// <summary>
        /// Path of the bloc illustration.
        /// </summary>
        [IgnoreDataMember] public string IllustrationPath
        {
            get
            {
                if (m_IllustrationPath.StartsWith("."))
                {
                    string localPath = m_IllustrationPath.Remove(0, 1);
                    return ApplicationState.ProjectLoadedPath + localPath;
                }
                else
                {
                    return m_IllustrationPath;
                }
            }
            set
            {
                if (m_IllustrationPath.StartsWith(ApplicationState.ProjectLoadedPath))
                {
                    m_IllustrationPath = "." + value.Remove(0, ApplicationState.ProjectLoadedPath.Length);
                }
                else
                {
                    m_IllustrationPath = value;
                }
            }
        }
        /// <summary>
        /// Sorting trials of the bloc.
        /// </summary>
        [DataMember] public string Sort { get; set; }
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
            return new Bloc(Name, Position, IllustrationPath, Sort, Window, Baseline, Events.ToArray().DeepClone().ToList() , Scenario.Clone() as Scenario, ID.Clone() as string);
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