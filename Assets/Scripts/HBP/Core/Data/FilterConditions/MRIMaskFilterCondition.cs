using HBP.Core.DLL;
using HBP.Core.Enums;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("MRI Mask"), SortingOrder(8), FilterCondition(typeof(Object3D.Site))]
    public class MRIMaskFilterCondition : BaseFilterCondition
    {
        #region Properties
        [JsonProperty("NiftiFilePath")] public string NiftiFilePath { get; set; } = "";
        [JsonProperty("ComparisonType")] public NumberComparisonType ComparisonType { get; set; }
        [JsonProperty("Value")] public float Value { get; set; }
        [JsonProperty("Min")] public float Min { get; set; }
        [JsonProperty("Max")] public float Max { get; set; }

        [JsonIgnore] private Volume m_LoadedVolume;

        public override string Description
        {
            get
            {
                if (string.IsNullOrEmpty(NiftiFilePath))
                    return "No NIfTI file selected";

                if (!File.Exists(NiftiFilePath))
                    return $"File not found: {Path.GetFileName(NiftiFilePath)}";

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

                return $"The voxel value at the site's position in '{Path.GetFileName(NiftiFilePath)}' is{(IsNot ? " not" : "")} {comparisonStr}";
            }
        }
        #endregion

        #region Constructors
        public MRIMaskFilterCondition() : this("", NumberComparisonType.Equal, 0, 0, 0, false) { }

        public MRIMaskFilterCondition(string niftiFilePath, NumberComparisonType comparisonType, float value, float min, float max, bool isNot) : base(isNot)
        {
            NiftiFilePath = niftiFilePath;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }

        public MRIMaskFilterCondition(string niftiFilePath, NumberComparisonType comparisonType, float value, float min, float max, bool isNot, string ID) : base(isNot, ID)
        {
            NiftiFilePath = niftiFilePath;
            ComparisonType = comparisonType;
            Value = value;
            Min = min;
            Max = max;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MRIMaskFilterCondition(NiftiFilePath, ComparisonType, Value, Min, Max, IsNot, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is MRIMaskFilterCondition mriMaskFilter)
            {
                NiftiFilePath = mriMaskFilter.NiftiFilePath;
                ComparisonType = mriMaskFilter.ComparisonType;
                Value = mriMaskFilter.Value;
                Min = mriMaskFilter.Min;
                Max = mriMaskFilter.Max;
            }
        }
        #endregion

        #region Public Methods
        public override void BeforeCheck()
        {
            if (string.IsNullOrEmpty(NiftiFilePath) || !File.Exists(NiftiFilePath))
            {
                m_LoadedVolume = null;
                return;
            }

            if (m_LoadedVolume == null)
            {
                m_LoadedVolume = new Volume();
                if (!m_LoadedVolume.LoadNIFTIFile(NiftiFilePath))
                {
                    m_LoadedVolume = null;
                }
            }
        }

        public override bool Check(object obj)
        {
            if (obj is Object3D.Site site)
            {
                if (m_LoadedVolume == null)
                    return false;

                float voxelValue = m_LoadedVolume.GetValueFromPosition(site.Information.DefaultPosition);

                bool result = ComparisonType switch
                {
                    NumberComparisonType.Equal => voxelValue == Value,
                    NumberComparisonType.Greater => voxelValue > Value,
                    NumberComparisonType.GreaterOrEqual => voxelValue >= Value,
                    NumberComparisonType.Lower => voxelValue < Value,
                    NumberComparisonType.LowerOrEqual => voxelValue <= Value,
                    NumberComparisonType.Range => voxelValue >= Min && voxelValue <= Max,
                    _ => false
                };

                return result != IsNot;
            }
            return false;
        }

        public override void AfterCheck()
        {
            m_LoadedVolume?.Dispose();
            m_LoadedVolume = null;
        }
        #endregion
    }
}