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
    *     - Order.
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
        [DataMember] public int Order { get; set; }
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
        /// <summary>
        /// The subBlocs of the bloc.
        /// </summary>
        [DataMember] public List<SubBloc> SubBlocs { get; set; }
        public SubBloc MainSubBloc
        {
            get
            {
                IOrderedEnumerable<SubBloc> orderedBlocs = SubBlocs.OrderBy((subBloc) => subBloc.Order);
                return orderedBlocs.FirstOrDefault();
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
    }
}