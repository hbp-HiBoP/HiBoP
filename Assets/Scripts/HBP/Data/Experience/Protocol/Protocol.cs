using System;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.Experience.Protocol
{
    /**
    * \class Protocol
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Protocol of a Experience.
    * 
    * \details Class which define a visualization protocol which contains : 
    *     - \a Unique \a ID.
    *     - \a Label.
    *     - \a Blocs.
    */
	public class Protocol : ICloneable,ICopiable
    {
        #region Properties
        public const string EXTENSION = ".prov";
        /// <summary>
        /// Unique ID.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Name of the protocol.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Blocs of the protocol.
        /// </summary>
        public List<Bloc> Blocs { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new protocol instance.
        /// </summary>
        /// <param name="name">Name of the protocol.</param>
        /// <param name="blocs">Blocs of the protocol.</param>
        /// <param name="id">Unique ID of the protocol.</param>
        public Protocol(string name,IEnumerable<Bloc> blocs,string id)
        {
            Name = name;
            Blocs = blocs.ToList();
            ID = id;
        }
        /// <summary>
        /// Create a new protocol instance.
        /// </summary>
        /// <param name="name">Name of the protocol.</param>
        /// <param name="blocs">Blocs of the protocol.</param>
        public Protocol(string name, IEnumerable<Bloc> blocs) : this(name,blocs, Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new protocol instance with default values.
        /// </summary>
        public Protocol() : this(string.Empty,new List<Bloc>())
		{
        }
        #endregion

        #region Operator
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="copy">instance to copy.</param>
        public void Copy(object copy)
        {
            Protocol protocol = copy as Protocol;
            ID = protocol.ID;
            Name = protocol.Name;
            Blocs = protocol.Blocs;
        }
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Instance cloned.</returns>
        public object Clone()
        {
            return new Protocol(Name.Clone() as string, new List<Bloc>(Blocs.ToArray().DeepClone()), ID.Clone() as string);
        }
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Protocol p = obj as Protocol;
            if (p == null)
            {
                return false;
            }
            else
            {
                return Name == p.Name && Enumerable.SequenceEqual(Blocs, p.Blocs);
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
        /// <param name="a">Display informations to compare.</param>
        /// <param name="b">Display informations to compare.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public static bool operator ==(Protocol a, Protocol b)
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
        /// <param name="a">Display informations to compare.</param>
        /// <param name="b">Display informations to compare.</param>
        /// <returns>\a True if not equals and \a false otherwise.</returns>
        public static bool operator !=(Protocol a, Protocol b)
        {
            return !(a == b);
        }
        #endregion
    }
}