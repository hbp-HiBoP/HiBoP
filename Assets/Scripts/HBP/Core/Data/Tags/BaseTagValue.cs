using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// A base class which contains all the data about a value and its associated tag.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Tag</b></term>
    /// <description>Tag associated with the value.</description>
    /// </item>
    /// <item>
    /// <term><b>Value</b></term>
    /// <description>Value associated with the tag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public class BaseTagValue : BaseData
    {
        #region Properties
        [DataMember(Name = "Tag")] protected string m_TagID;
        /// <summary>
        /// Tag associated with the value.
        /// </summary>
        public BaseTag Tag { get; set; }

        [DataMember(Name = "Value")] protected object m_Value;
        /// <summary>
        /// Value associated with the tag.
        /// </summary>
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
        
        /// <summary>
        /// Value in its displayable form.
        /// </summary>
        public virtual string DisplayableValue
        {
            get
            {
                return m_Value.ToString();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of BaseTagValue.
        /// </summary>
        public BaseTagValue() : this(null, null)
        {

        }
        /// <summary>
        /// Create a new instance of BaseTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        public BaseTagValue(BaseTag tag) : this(tag, null)
        {

        }
        /// <summary>
        /// Create a new instance of BaseTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public BaseTagValue(BaseTag tag, object value) : base()
        {
            Tag = tag;
            Value = value;
        }
        /// <summary>
        /// Create a new instance of BaseTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public BaseTagValue(BaseTag tag, object value, string ID) : base(ID)
        {
            Tag = tag;
            Value = value;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the value.
        /// </summary>
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
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            Value = Value;
            Tag = ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.ID == m_TagID);
        }
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_TagID = Tag.ID;
        }
        #endregion
    }
}