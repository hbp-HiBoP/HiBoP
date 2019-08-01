using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.Unity;
using UnityEngine;

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
    *     - Order.
    *     - Image.
    *     - Sorting.
    */
    [DataContract]
    public class Bloc : ICloneable, ICopiable, IIdentifiable
	{
        #region Enums
        public enum SortingMethodError { NoError, NoSortingConditionFound, InvalidNumberOfElements, SubBlocNotFound, EventNotFound, InvalidCommand }
        #endregion

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
        [DataMember] public int Order { get; set; }
        [DataMember(Name = "IllustrationPath")] string m_IllustrationPath = "";
        /// <summary>
        /// Path of the bloc illustration.
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
                m_NeedToReload = true;
            }
        }

        bool m_NeedToReload;
        Sprite m_Image;
        public Sprite Image
        {
            get
            {
                if(m_NeedToReload || m_Image != null)
                {
                    if (SpriteExtension.LoadSpriteFromFile(out Sprite sprite, IllustrationPath)) m_Image = sprite;
                }
                return m_Image;
            }
        }
        /// <summary>
        /// Sorting trials of the bloc.
        /// </summary>
        [DataMember] public string Sort { get; set; }
        /// <summary>
        /// The subBlocs of the bloc.
        /// </summary>
        [DataMember] public List<SubBloc> SubBlocs { get; set; }
        public SubBloc MainSubBloc
        {
            get
            {
                return SubBlocs.FirstOrDefault(s => s.Type == Enums.MainSecondaryEnum.Main);
            }
        }
        public IOrderedEnumerable<SubBloc> OrderedSubBlocs
        {
            get
            {
                return SubBlocs.OrderBy(s => s.Order).ThenBy(s => s.Name);
            }
        }
        public int MainSubBlocPosition
        {
            get
            {
                return Array.IndexOf(OrderedSubBlocs.ToArray(), MainSubBloc);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="name">Name of the bloc.</param>
        /// <param name="order">Order of the bloc in the trial matrix.</param>
        /// <param name="illustrationPath">Illustation path of the bloc.</param>
        /// <param name="sort">Sorting  of the trials in the bloc.</param>
        /// <param name="subBlocs">SubBlocs of the bloc.</param>
        /// <param name="id">Unique ID of the bloc.</param>
        public Bloc(string name, int order, string illustrationPath, string sort, IEnumerable<SubBloc> subBlocs, string id)
        {
            ID = id;
            Name = name;
            Order = order;
            IllustrationPath = illustrationPath;
            Sort = sort;
            SubBlocs = subBlocs.ToList();
        }
        /// <summary>
        /// Create a new bloc instance with a unique ID.
        /// </summary>
        /// <param name="name">Name of the bloc.</param>
        /// <param name="order">Order of the bloc in the trial matrix.</param>
        /// <param name="illustrationPath">Illustation path of the bloc.</param>
        /// <param name="sort">Sorting  of the trials in the bloc.</param>
        /// <param name="subBlocs">SubBlocs of the bloc.</param>
        public Bloc(string name, int order, string illustrationPath, string sort, IEnumerable<SubBloc> subBlocs) : this(name, order, illustrationPath, sort, subBlocs, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new bloc instance at a position with default values.
        /// </summary>
        public Bloc(int order) : this(string.Empty, order, string.Empty, string.Empty, new List<SubBloc>())
		{
		}
        /// <summary>
        /// Create a new blocs instance with default values.
        /// </summary>
        public Bloc() : this (0)
        {

        }
        #endregion

        #region Operators
        public void GenerateNewIDs()
        {
            ID = Guid.NewGuid().ToString();
            foreach (var subBloc in SubBlocs) subBloc.GenerateNewIDs();
        }

        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        public void Copy(object copy)
        {
            Bloc bloc = copy as Bloc;

            ID = bloc.ID;
            Name = bloc.Name;
            Order = bloc.Order;
            IllustrationPath = bloc.IllustrationPath;
            Sort = bloc.Sort;
            SubBlocs = bloc.SubBlocs;
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public object Clone()
        {
            return new Bloc(Name, Order, IllustrationPath, Sort, SubBlocs.ToArray().DeepClone(), ID.Clone() as string);
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

        #region Serialization
        [OnDeserialized()]
        public void OnDeserialized(StreamingContext context)
        {
            m_IllustrationPath = m_IllustrationPath.ToPath();
        }
        #endregion

        #region Public Methods
        public SortingMethodError GetSortingMethodError()
        {
            string[] orders = Sort.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < orders.Length; i++)
            {
                string order = orders[i];
                string[] parts = order.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    string subBlocName = parts[0];
                    string eventName = parts[1];
                    string command = parts[2];
                    SubBloc subBloc = SubBlocs.FirstOrDefault(s => s.Name == subBlocName);
                    if (subBloc != null)
                    {
                        Event @event = subBloc.Events.FirstOrDefault(e => e.Name == eventName);
                        if (@event != null)
                        {
                            if (command == "LATENCY" || command == "CODE")
                            {
                                return SortingMethodError.NoError;
                            }
                            else
                            {
                                return SortingMethodError.InvalidCommand;
                            }
                        }
                        else
                        {
                            return SortingMethodError.EventNotFound;
                        }
                    }
                    else
                    {
                        return SortingMethodError.SubBlocNotFound;
                    }
                }
                else
                {
                    return SortingMethodError.InvalidNumberOfElements;
                }
            }
            return SortingMethodError.NoSortingConditionFound;
        }
        public string GetSortingMethodErrorMessage(SortingMethodError error)
        {
            switch (error)
            {
                case SortingMethodError.NoError:
                    return "No error detected.";
                case SortingMethodError.NoSortingConditionFound:
                    return "No sorting condition found.";
                case SortingMethodError.InvalidNumberOfElements:
                    return "Invalid number of elements (must be 3 elements per condition).";
                case SortingMethodError.SubBlocNotFound:
                    return "Sub bloc not found.";
                case SortingMethodError.EventNotFound:
                    return "Event not found within sub bloc.";
                case SortingMethodError.InvalidCommand:
                    return "Command is invalid (must be \"CODE\" or \"LATENCY\").";
                default:
                    return "Unknown error.";
            }
        }
        public static int GetNumberOfColumns(IEnumerable<Bloc> blocs)
        {
            int before = 0;
            int after = 0;
            foreach (var bloc in blocs)
            {
                int mainPosition = bloc.MainSubBlocPosition;
                before = Mathf.Max(before, mainPosition);
                after = Mathf.Max(after, bloc.SubBlocs.Count - 1 - mainPosition);
            }
            return before + 1 + after;
        }
        public static Tuple<SubBloc[], Window>[] GetSubBlocsAndWindowByColumn(IEnumerable<Bloc> blocs)
        {
            List<Tuple<int, List<SubBloc>>> subBlocsByColumns = new List<Tuple<int, List<SubBloc>>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                SubBloc[] orderedSubBlocs = bloc.OrderedSubBlocs.ToArray();
                for (int i = 0; i < orderedSubBlocs.Length; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.Any(t => t.Item1 == column)) subBlocsByColumns.Add(new Tuple<int, List<SubBloc>>(column, new List<SubBloc>()));
                    subBlocsByColumns.Find(t => t.Item1 == column).Item2.Add(orderedSubBlocs[i]);
                }
            }
            subBlocsByColumns = subBlocsByColumns.OrderBy(t => t.Item1).ToList();

            List<Tuple<SubBloc[], Tools.CSharp.Window>> timeLimitsByColumns = new List<Tuple<SubBloc[], Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                Window window = new Window(tuple.Item2.Min(s => s.Window.Start), tuple.Item2.Max(s => s.Window.End));
                timeLimitsByColumns.Add(new Tuple<SubBloc[], Window>(tuple.Item2.ToArray(), window));
            }
            return timeLimitsByColumns.ToArray();
        }
        #endregion
    }
}