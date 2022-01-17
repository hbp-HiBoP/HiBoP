using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Tools.CSharp;
using Tools.Unity;
using UnityEngine;

namespace HBP.Data.Experience.Protocol
{
    /// <summary>
    /// Class which contains all the data about a experience bloc used to epoch, and visualize data.
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
    /// <description>Name of the bloc</description>
    /// </item>
    /// <item>
    /// <term><b>Order</b></term> 
    /// <description>Order of the blocs in the protocol. Used to display protocol trialMatrix</description>
    /// </item>
    /// <item>
    /// <term><b>IllustrationPath</b></term> 
    /// <description>Bloc illustration path</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Bloc : BaseData, INameable
    {
        #region Properties
        public enum SortingMethodError { NoError, NoSortingConditionFound, InvalidNumberOfElements, SubBlocNotFound, EventNotFound, InvalidCommand }
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
        [IgnoreDataMember]
        public string IllustrationPath
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
        /// <summary>
        /// True if the image need to be reload from the illustration path. False otherwise.
        /// </summary>
        bool m_NeedToReload;
        Sprite m_Image;
        /// <summary>
        /// Image loaded from the illustration path.
        /// </summary>
        public Sprite Image
        {
            get
            {
                if (m_NeedToReload || m_Image != null)
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
        /// <summary>
        /// Main subBloc of the bloc.
        /// </summary>
        public SubBloc MainSubBloc
        {
            get
            {
                return SubBlocs.FirstOrDefault(s => s.Type == Enums.MainSecondaryEnum.Main);
            }
        }
        /// <summary>
        /// Subblocs ordered by SubBloc.Order.
        /// </summary>
        public IOrderedEnumerable<SubBloc> OrderedSubBlocs
        {
            get
            {
                return SubBlocs.OrderBy(s => s.Order).ThenBy(s => s.Name);
            }
        }
        /// <summary>
        /// Position of the main subBloc.
        /// </summary>
        public int MainSubBlocPosition
        {
            get
            {
                return Array.IndexOf(OrderedSubBlocs.ToArray(), MainSubBloc);
            }
        }
        /// <summary>
        /// True if the bloc is visualizable, False otherwise.
        /// </summary>
        public bool IsVisualizable
        {
            get
            {
                return MainSubBloc != null && SubBlocs.All(s => s.IsVisualizable);
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="name">Name of the bloc</param>
        /// <param name="order">Order of the bloc in the trial matrix</param>
        /// <param name="illustrationPath">Illustration path of the bloc</param>
        /// <param name="sort">Sorting  of the trials in the bloc</param>
        /// <param name="subBlocs">SubBlocs of the bloc</param>
        /// <param name="ID">Unique identifier of the bloc</param>
        public Bloc(string name, int order, string illustrationPath, string sort, IEnumerable<SubBloc> subBlocs, string ID) : base(ID)
        {
            Name = name;
            Order = order;
            IllustrationPath = illustrationPath;
            Sort = sort;
            SubBlocs = subBlocs.ToList();
        }
        /// <summary>
        /// Create a new bloc instance.
        /// </summary>
        /// <param name="name">Name of the bloc</param>
        /// <param name="order">Order of the bloc in the trial matrix</param>
        /// <param name="illustrationPath">Illustration path of the bloc</param>
        /// <param name="sort">Sorting  of the trials in the bloc</param>
        /// <param name="subBlocs">SubBlocs of the bloc</param>
        public Bloc(string name, int order, string illustrationPath, string sort, IEnumerable<SubBloc> subBlocs) : base()
        {
            Name = name;
            Order = order;
            IllustrationPath = illustrationPath;
            Sort = sort;
            SubBlocs = subBlocs.ToList();
        }
        /// <summary>
        /// Create a new bloc instance at a position with default values.
        /// </summary>
        public Bloc(int order) : this("New bloc", order, string.Empty, string.Empty, new List<SubBloc>())
        {
        }
        /// <summary>
        /// Create a new blocs instance with default values.
        /// </summary>
        public Bloc() : this(0)
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get all the errors from the sorting.
        /// </summary>
        /// <returns>Sorting error</returns>
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
        /// <summary>
        /// Generate unique identifier.
        /// </summary>
        public override void GenerateID()
        {
            base.GenerateID();
            foreach (var subBloc in SubBlocs) subBloc.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            foreach (var subBloc in SubBlocs) IDs.AddRange(subBloc.GetAllIdentifiable());
            return IDs;
        }
        /// <summary>
        /// Get sorting method error in a displayable form.
        /// </summary>
        /// <param name="error">Sorting method errors</param>
        /// <returns>All sorting method errors in a displyable form</returns>
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
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Get number of trialMatrix columns to display these blocs.
        /// </summary>
        /// <param name="blocs">Blocs to display</param>
        /// <returns>Number of columns needed</returns>
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
        /// <summary>
        /// Get subBlocs and window by column.
        /// </summary>
        /// <param name="blocs">Blocs to display</param>
        /// <returns>Tuple containing a array of tuple of bloc and subbloc</returns>
        public static Tuple<Tuple<Bloc,SubBloc>[], Window>[] GetSubBlocsAndWindowByColumn(IEnumerable<Bloc> blocs)
        {
            List<Tuple<int, List<Tuple<Bloc, SubBloc>>>> subBlocsByColumns = new List<Tuple<int, List<Tuple<Bloc, SubBloc>>>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                SubBloc[] orderedSubBlocs = bloc.OrderedSubBlocs.ToArray();
                for (int i = 0; i < orderedSubBlocs.Length; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.Any(t => t.Item1 == column)) subBlocsByColumns.Add(new Tuple<int, List<Tuple<Bloc, SubBloc>>>(column, new List<Tuple<Bloc, SubBloc>>()));
                    subBlocsByColumns.Find(t => t.Item1 == column).Item2.Add(new Tuple<Bloc, SubBloc>(bloc, orderedSubBlocs[i]));
                }
            }
            subBlocsByColumns = subBlocsByColumns.OrderBy(t => t.Item1).ToList();

            List<Tuple<Tuple<Bloc,SubBloc>[], Window>> timeLimitsByColumns = new List<Tuple<Tuple<Bloc,SubBloc>[], Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                Window window = new Window(tuple.Item2.Min(s => s.Item2.Window.Start), tuple.Item2.Max(s => s.Item2.Window.End));
                timeLimitsByColumns.Add(new Tuple<Tuple<Bloc,SubBloc>[], Window>(tuple.Item2.ToArray(), window));
            }
            return timeLimitsByColumns.ToArray();
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
            if (obj is Bloc bloc)
            {
                Name = bloc.Name;
                Order = bloc.Order;
                IllustrationPath = bloc.IllustrationPath;
                Sort = bloc.Sort;
                SubBlocs = bloc.SubBlocs;
            }
        }
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Bloc(Name, Order, IllustrationPath, Sort, SubBlocs.DeepClone(), ID);
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            m_IllustrationPath = m_IllustrationPath.StandardizeToEnvironement();
        }
        #endregion
    }
}