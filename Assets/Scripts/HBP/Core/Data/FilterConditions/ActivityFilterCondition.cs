using HBP.Core.DLL;
using HBP.Core.Enums;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Signal measure"), SortingOrder(8), FilterCondition(typeof(Object3D.Site))]
    public class ActivityFilterCondition : BaseFilterCondition
    {
        #region Properties

        [JsonProperty("MeasureType")] public MeasureType MeasureType { get; set; }
        [JsonProperty("ComparisonType")] public NumberComparisonType ComparisonType { get; set; }
        [JsonProperty("Value")] public float Value { get; set; }
        [JsonProperty("Min")] public float Min { get; set; }
        [JsonProperty("Max")] public float Max { get; set; }

        public override string Description
        {
            get
            {
                string valueTypeStr = MeasureType.ToString();
                string comparisonStr = ComparisonType switch
                {
                    NumberComparisonType.Equal => $"equal to {Value}",
                    NumberComparisonType.Greater => $"greater than {Value}",
                    NumberComparisonType.GreaterOrEqual => $"greater or equal to {Value}",
                    NumberComparisonType.Lower => $"lower than {Value}",
                    NumberComparisonType.LowerOrEqual => $"lower or equal to {Value}",
                    NumberComparisonType.Range => $"between {Min} and {Max} (inclusive)",
                    _ => ""
                };
                return $"The {valueTypeStr} of this site's activity is{(IsNot ? " not" : "")} {comparisonStr}";
            }
        }

        #endregion

        #region Constructors

        public ActivityFilterCondition() : this(MeasureType.Mean, NumberComparisonType.Equal, 0, 0, 0, false)
        {
        }

        public ActivityFilterCondition(MeasureType valueType, NumberComparisonType comparisonType, float value, float min, float max, bool isNot) : base(isNot)
        {
            MeasureType = valueType;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }

        public ActivityFilterCondition(MeasureType valueType, NumberComparisonType comparisonType, float value, float min, float max, bool isNot, string ID) : base(isNot, ID)
        {
            MeasureType = valueType;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }

        #endregion

        #region Operators

        public override object Clone()
        {
            return new ActivityFilterCondition(MeasureType, ComparisonType, Value, Min, Max, IsNot, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is ActivityFilterCondition activityFilter)
            {
                MeasureType = activityFilter.MeasureType;
                ComparisonType = activityFilter.ComparisonType;
                Value = activityFilter.Value;
                Min = activityFilter.Min;
                Max = activityFilter.Max;
            }
        }

        #endregion

        #region Public Methods

        public override bool Check(object obj)
        {
            if (obj is Object3D.Site site)
            {
                if (site.Statistics == null)
                    return false;

                float[] allValues = site.Statistics.Trial.AllValues;

                if (allValues.Length == 0)
                    return false;

                float activityValue = MeasureType switch
                {
                    MeasureType.Mean => allValues.Mean(),
                    MeasureType.Median => allValues.Median(),
                    MeasureType.Min => allValues.Min(),
                    MeasureType.Max => allValues.Max(),
                    MeasureType.StandardDeviation => allValues.StandardDeviation(),
                    _ => float.MinValue
                };

                if (activityValue == float.MinValue)
                    return false;

                bool result = ComparisonType switch
                {
                    NumberComparisonType.Equal => activityValue == Value,
                    NumberComparisonType.Greater => activityValue > Value,
                    NumberComparisonType.GreaterOrEqual => activityValue >= Value,
                    NumberComparisonType.Lower => activityValue < Value,
                    NumberComparisonType.LowerOrEqual => activityValue <= Value,
                    NumberComparisonType.Range => activityValue >= Min && activityValue <= Max,
                    _ => false
                };

                return result != IsNot;
            }

            return false;
        }

        #endregion
    }
}
