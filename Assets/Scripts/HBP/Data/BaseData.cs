using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HBP.Data
{
    /// <summary>
    /// Abstract base class for every HiBoP data.
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
    /// </list>
    /// </remarks>
    [DataContract]
    public abstract class BaseData: ICopiable, ICloneable, IIdentifiable
    {
        #region Properties
        /// <summary>
        /// Unique identifier to identify the data.
        /// </summary>
        [DataMember] public string ID { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new BaseData instance.
        /// </summary>
        public BaseData() : this(Guid.NewGuid().ToString())
        {
        }
        /// <summary>
        /// Create a new BaseData instance.
        /// </summary>
        /// <param name="ID">Unique indentifier</param>
        public BaseData(string ID)
        {
            this.ID = ID;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Generate a new unique identifier.
        /// </summary>
        public virtual void GenerateID()
        {
            ID = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// Returns a list of all IDs of this object (including itself)
        /// </summary>
        /// <returns></returns>
        public virtual List<BaseData> GetAllIdentifiable()
        {
            return new List<BaseData>() { this };
        }
        #endregion

        #region Operators
        /// <summary>
        /// Operator Equals.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>\a True if equals and \a false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is BaseData baseData)
            {
                return ID == baseData.ID;
            }
            else
            {
                return false;
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
        public static bool operator ==(BaseData a, BaseData b)
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
        public static bool operator !=(BaseData a, BaseData b)
        {
            return !(a == b);
        }
        public abstract object Clone();
        public virtual void Copy(object copy)
        {
            if(copy is BaseData baseData)
            {
                ID = baseData.ID;
            }
        }
        #endregion

        #region Serialization
        [OnSerializing()]
        internal void OnSerializingMethod(StreamingContext context)
        {
            OnSerializing();
        }
        [OnSerialized()]
        internal void OnSerializedMethod(StreamingContext context)
        {
            OnSerialized();
        }
        [OnDeserializing()]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            OnDeserializing();
        }
        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            OnDeserialized();
        }
        /// <summary>
        /// Called on OnSerializing(). You can override this function and use this to do anything needed before serializing.
        /// </summary>
        protected virtual void OnSerializing()
        {

        }
        /// <summary>
        /// Called on OnSerialized(). You can override this function and use this to do anything needed after serializing.
        /// </summary>
        protected virtual void OnSerialized()
        {

        }
        /// <summary>
        /// Called on OnDeserializing(). You can override this function and use this to do anything needed before deserializing.
        /// </summary>
        protected virtual void OnDeserializing()
        {

        }
        /// <summary>
        /// Called on OnDeserialized(). You can override this function and use this to do anything needed before deserializing.
        /// </summary>
        protected virtual void OnDeserialized()
        {

        }
        #endregion
    }
}
