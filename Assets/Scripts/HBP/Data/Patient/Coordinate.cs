using System.Runtime.Serialization;
using UnityEngine;

namespace HBP.Data.Anatomy
{
    [DataContract]
    public class Coordinate : BaseData
    {
        #region Properties
        /// <summary>
        /// Reference system of the coordinate.
        /// </summary>
        [DataMember] public string ReferenceSystem { get; set; }
        /// <summary>
        /// Value of the coordinate.
        /// </summary>
        [DataMember] public SerializableVector3 Value { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the coordinate class.
        /// </summary>
        /// <param name="referenceSystem">Reference system of the coordinate.</param>
        /// <param name="value">Value of the coordinate.</param>
        /// <param name="ID">Unique identifier to identify the coordinate.</param>
        public Coordinate(string referenceSystem, Vector3 value, string ID) : base(ID)
        {
            ReferenceSystem = referenceSystem;
            Value = new SerializableVector3(value);
        }
        /// <summary>
        /// Initializes a new instance of the coordinate class.
        /// </summary>
        /// <param name="name">Name of the site.</param>
        /// <param name="tags">Tags of the site.</param>
        public Coordinate(string name, Vector3 value) : base()
        {
            ReferenceSystem = name;
            Value = new SerializableVector3(value);
        }
        /// <summary>
        /// Initializes a new instance of the coordinate class.
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
            return new Coordinate(ReferenceSystem, Value.ToVector3(), ID);
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
                Value = coordinate.Value;
            }
        }
        #endregion
    }
}