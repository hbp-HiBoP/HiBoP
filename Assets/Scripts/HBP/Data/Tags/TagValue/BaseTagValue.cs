using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Tags
{
    [DataContract]
    public class BaseTagValue : BaseData
    {
        #region Properties
        [DataMember(Name = "Tag")] protected string m_TagID;
        public Tag Tag { get; protected set; }

        [DataMember(Name = "Value")] protected object m_Value;
        public object Value
        {
            get
            {
                return m_Value;
            }
            protected set
            {
                m_Value = value;
            }
        }

        public virtual string DisplayableValue
        {
            get
            {
                return m_Value.ToString();
            }
        }
        #endregion

        #region Constructors
        public BaseTagValue() : this(null, null)
        {

        }
        public BaseTagValue(Tag tag) : this(tag, null)
        {

        }
        public BaseTagValue(Tag tag, object value) : base()
        {
            Tag = tag;
            Value = value;
        }
        public BaseTagValue(Tag tag, object value, string ID) : base(ID)
        {
            Tag = tag;
            Value = value;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone the instance.
        /// </summary>
        /// <returns>object cloned.</returns>
        public override object Clone()
        {
            return new BaseTagValue(Tag, Value, ID);
        }
        #endregion

        #region Serialization
        [OnSerializing()]
        protected void OnSerializingMethod(StreamingContext context)
        {
            m_TagID = Tag?.ID;
        }
        [OnDeserialized()]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            Tag = ApplicationState.ProjectLoaded.Settings.Tags.FirstOrDefault(t => t.ID == m_TagID);
            Value = Value;
        }
    #endregion
    }
}