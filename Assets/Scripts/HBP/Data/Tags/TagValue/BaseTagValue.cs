using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data
{
    [DataContract]
    public class BaseTagValue : BaseData
    {
        #region Properties
        [DataMember(Name = "BaseTag")] protected string m_TagID;
        public BaseTag Tag
        {
            get
            {
                return ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.ID == m_TagID);
            }
            set
            {
                m_TagID = value.ID;
            }
        }

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
        public BaseTagValue(BaseTag tag) : this(tag, null)
        {

        }
        public BaseTagValue(BaseTag tag, object value) : base()
        {
            m_TagID = tag?.ID;
            Value = value;
        }
        public BaseTagValue(BaseTag tag, object value, string ID) : base(ID)
        {
            m_TagID = tag?.ID;
            Value = value;
        }
        #endregion

        #region Public Methods
        public void UpdateValue()
        {
            Value = Value;
        }
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
        [OnDeserialized()]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            Value = Value;
        }
        #endregion
    }
}