using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class FilterConditionsPreset : BaseData
    {
        #region Properties

        [JsonProperty] public string Name { get; set; }
        [JsonProperty] public List<BaseFilterCondition> Conditions { get; set; }

        #endregion

        #region Constructors

        public FilterConditionsPreset() : this("", new List<BaseFilterCondition>())
        {
        }

        public FilterConditionsPreset(IEnumerable<BaseFilterCondition> conditions) : this("", conditions)
        {
        }

        public FilterConditionsPreset(string name, IEnumerable<BaseFilterCondition> conditions) : base()
        {
            Name = name;
            Conditions = conditions.ToList();
        }

        public FilterConditionsPreset(string name, IEnumerable<BaseFilterCondition> conditions, string ID) : base(ID)
        {
            Name = name;
            Conditions = conditions.ToList();
        }

        #endregion

        #region Operators

        public override object Clone()
        {
            return new FilterConditionsPreset(Name, Conditions.DeepClone(), ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is FilterConditionsPreset filterGroup)
            {
                Name = filterGroup.Name;
                Conditions = filterGroup.Conditions.DeepClone().ToList();
            }
        }

        #endregion
    }
}
