using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("All of"), SortingOrder(100), FilterCondition]
    public class AllFilterCondition : BaseFilterCondition
    {
        #region Properties

        public override string Description
        {
            get
            {
                if (Conditions == null || Conditions.Count < 2)
                    return "Invalid condition: at least 2 sub-conditions are required";
                var descriptions = Conditions.Select(c => c.Description).ToList();
                return $"{(IsNot ? "NOT " : "")}({string.Join(" AND ", descriptions)})";
            }
        }

        [JsonProperty("Conditions")] public List<BaseFilterCondition> Conditions { get; set; } = new List<BaseFilterCondition>();

        #endregion

        #region Constructors

        public AllFilterCondition() : this(new List<BaseFilterCondition>(), false)
        {
        }

        public AllFilterCondition(IEnumerable<BaseFilterCondition> conditions, bool isNot) : base(isNot)
        {
            Conditions = conditions.ToList();
        }

        public AllFilterCondition(IEnumerable<BaseFilterCondition> conditions, bool isNot, string ID) : base(isNot, ID)
        {
            Conditions = conditions.ToList();
        }

        #endregion

        #region Operators

        public override object Clone()
        {
            return new AllFilterCondition(Conditions.Select(c => (BaseFilterCondition)c.Clone()), IsNot, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AllFilterCondition all)
            {
                Conditions = all.Conditions.Select(c => (BaseFilterCondition)c.Clone()).ToList();
            }
        }

        #endregion

        #region Public Methods

        public override bool Check(object obj)
        {
            if (Conditions == null || Conditions.Count < 2)
                return false;

            bool result = Conditions.All(c => c.Check(obj));
            return result != IsNot;
        }

        #endregion
    }
}
