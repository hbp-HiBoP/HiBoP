using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Any of"), SortingOrder(101), FilterCondition(typeof(object))]
    public class AnyFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Conditions == null || Conditions.Count < 2)
                    return "Invalid condition: at least 2 sub-conditions are required";
                var descriptions = Conditions.Select(c => c.Description).ToList();
                return $"{(IsNot ? "NOT " : "")}({string.Join(" OR ", descriptions)})";
            }
        }
        [JsonProperty("Conditions")] public List<BaseFilterCondition> Conditions { get; set; } = new List<BaseFilterCondition>();
        #endregion

        #region Constructors
        public AnyFilterCondition() : this(new List<BaseFilterCondition>(), false) { }
        public AnyFilterCondition(IEnumerable<BaseFilterCondition> conditions, bool isNot) : base(isNot)
        {
            Conditions = conditions.ToList();
        }
        public AnyFilterCondition(IEnumerable<BaseFilterCondition> conditions, bool isNot, string ID) : base(isNot, ID)
        {
            Conditions = conditions.ToList();
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new AnyFilterCondition(Conditions.Select(c => (BaseFilterCondition)c.Clone()), IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AnyFilterCondition any)
            {
                Conditions = any.Conditions.Select(c => (BaseFilterCondition)c.Clone()).ToList();
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(BaseData obj)
        {
            if (Conditions == null || Conditions.Count < 2)
                return false;

            bool result = Conditions.Any(c => c.Check(obj));
            return result != IsNot;
        }
        #endregion
    }
}