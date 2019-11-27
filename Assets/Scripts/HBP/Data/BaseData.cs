using System;
using System.Runtime.Serialization;

namespace HBP.Data
{
    [DataContract]
    public abstract class BaseData: ICopiable, ICloneable, IIdentifiable
    {
        #region Properties
        [DataMember] public string ID { get; set; }
        #endregion

        #region Constructors
        public BaseData() : this(Guid.NewGuid().ToString())
        {
        }
        public BaseData(string ID)
        {
            this.ID = ID;
        }
        #endregion

        #region Public Methods
        public virtual void GenerateID()
        {
            ID = Guid.NewGuid().ToString();
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
        protected virtual void OnSerializing()
        {

        }
        protected virtual void OnSerialized()
        {

        }
        protected virtual void OnDeserializing()
        {

        }
        protected virtual void OnDeserialized()
        {

        }
        #endregion
    }
}
