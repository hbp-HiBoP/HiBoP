using HBP.Core.Enums;
using HBP.Core.Object3D;
using Newtonsoft.Json;
using System.ComponentModel;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Raw position"), SortingOrder(7), FilterCondition(typeof(Object3D.Site))]
    public class RawSitePositionFilterCondition : BaseFilterCondition
    {
        #region Enums
        public enum AxisType { X, Y, Z }
        #endregion

        #region Properties
        [JsonProperty("Axis")] public AxisType Axis { get; set; }
        [JsonProperty("ComparisonType")] public NumberComparisonType ComparisonType { get; set; }
        [JsonProperty("Value")] public float Value { get; set; }
        [JsonProperty("Min")] public float Min { get; set; }
        [JsonProperty("Max")] public float Max { get; set; }

        public override string Description
        {
            get
            {
                string axisStr = Axis.ToString();
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
                return $"The {axisStr} position of the site is{(IsNot ? " not" : "")} {comparisonStr}";
            }
        }
        #endregion

        #region Constructors
        public RawSitePositionFilterCondition() : this(AxisType.X, NumberComparisonType.Equal, 0, 0, 0, false) { }
        public RawSitePositionFilterCondition(AxisType axis, NumberComparisonType comparisonType, float value, float min, float max, bool isNot)
            : base(isNot)
        {
            Axis = axis;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }
        public RawSitePositionFilterCondition(AxisType axis, NumberComparisonType comparisonType, float value, float min, float max, bool isNot, string ID)
            : base(isNot, ID)
        {
            Axis = axis;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new RawSitePositionFilterCondition(Axis, ComparisonType, Value, Min, Max, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is RawSitePositionFilterCondition other)
            {
                Axis = other.Axis;
                ComparisonType = other.ComparisonType;
                Value = other.Value;
                Min = other.Min;
                Max = other.Max;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is Object3D.Site site)
            {
                float axisValue = Axis switch
                {
                    AxisType.X => -site.Information.DefaultPosition.x,
                    AxisType.Y => site.Information.DefaultPosition.y,
                    AxisType.Z => site.Information.DefaultPosition.z,
                    _ => float.MinValue
                };

                if (axisValue == float.MinValue)
                    return false;

                bool result = ComparisonType switch
                {
                    NumberComparisonType.Equal => axisValue == Value,
                    NumberComparisonType.Greater => axisValue > Value,
                    NumberComparisonType.GreaterOrEqual => axisValue >= Value,
                    NumberComparisonType.Lower => axisValue < Value,
                    NumberComparisonType.LowerOrEqual => axisValue <= Value,
                    NumberComparisonType.Range => axisValue >= Min && axisValue <= Max,
                    _ => false
                };

                return result != IsNot;
            }
            return false;
        }
        #endregion
    }
}