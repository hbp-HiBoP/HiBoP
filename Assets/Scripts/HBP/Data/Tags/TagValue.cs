namespace HBP.Data.Tags
{
    public class TagValue : BaseData
    {
        #region Properties
        public Tag Tag { get; set; }
        public object Value { get; set; }
        #endregion

        #region Constructors
        public TagValue() : this(null, null)
        {

        }
        public TagValue(Tag tag) : this(tag, null)
        {

        }
        public TagValue(Tag tag, object value) : base()
        {
            Tag = tag;
            Value = value;
        }
        public TagValue(Tag tag, object value, string id) : base(id)
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
            return new TagValue(Tag, Value);
        }
        /// <summary>
        /// Copy the instance.
        /// </summary>
        /// <param name="obj">instance to copy.</param>
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is TagValue tagValue)
            {
                Tag = tagValue.Tag;
                Value = tagValue.Value;
            }
        }
        #endregion
    }
}