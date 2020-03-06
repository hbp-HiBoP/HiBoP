using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data
{
    /// <summary>
    /// Class which contains all the data about a position in a specific reference system.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>ReferenceSystem</b></term> 
    /// <description>Reference system of the coordinate.</description>
    /// </item>
    /// <item>
    /// <term><b>Value</b></term> 
    /// <description>Position in the reference system.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class Coordinate : BaseData
    {
        #region Properties
        /// <summary>
        /// Reference system of the coordinate.
        /// </summary>
        [DataMember] public string ReferenceSystem { get; set; }
        /// <summary>
        /// Position in the reference system.
        /// </summary>
        [DataMember] public SerializableVector3 Position { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of coordinate.
        /// </summary>
        /// <param name="referenceSystem">Reference system of the coordinate</param>
        /// <param name="position">position in the reference system</param>
        /// <param name="ID">Unique identifier</param>
        public Coordinate(string referenceSystem, Vector3 position, string ID) : base(ID)
        {
            ReferenceSystem = referenceSystem;
            Position = new SerializableVector3(position);
        }
        /// <summary>
        /// Create a new instance of coordinate.
        /// </summary>
        /// <param name="referenceSystem">Reference system of the coordinate</param>
        /// <param name="position">position in the reference system</param>
        public Coordinate(string referenceSystem, Vector3 position) : base()
        {
            ReferenceSystem = referenceSystem;
            Position = new SerializableVector3(position);
        }
        /// <summary>
        /// Create a new instance of coordinate.
        /// </summary>
        public Coordinate() : this("Unknown", new Vector3())
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new Coordinate(ReferenceSystem, Position.ToVector3(), ID);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object obj)
        {
            base.Copy(obj);
            if (obj is Coordinate coordinate)
            {
                ReferenceSystem = coordinate.ReferenceSystem;
                Position = coordinate.Position;
            }
        }
        #endregion
    }
}