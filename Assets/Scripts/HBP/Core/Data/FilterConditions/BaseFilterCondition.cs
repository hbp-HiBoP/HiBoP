using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BaseFilterCondition : BaseData
    {
        #region Properties
        public virtual string Description => "";

        [JsonProperty("IsNot")] public bool IsNot { get; set; }
        #endregion

        #region Constructors
        public BaseFilterCondition() : this(false)
        {
        }
        public BaseFilterCondition(bool isNot) : base()
        {
            IsNot = isNot;
        }
        public BaseFilterCondition(bool isNot, string ID) : base(ID)
        {
            IsNot = isNot;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new BaseFilterCondition(IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseFilterCondition filterCondition)
            {
                IsNot = filterCondition.IsNot;
            }
        }
        #endregion

        #region Public Methods
        public virtual bool Check(BaseData obj)
        {
            return true;
        }
        #endregion
    }
}