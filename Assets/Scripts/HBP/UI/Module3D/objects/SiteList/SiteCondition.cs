using CielaSpike;
using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SiteCondition
    {
        #region Properties
        public enum ConditionTarget { Mean, Median, StandardDeviation, Max, Min }
        public enum ConditionComparator { Superior, Inferior }

        public ConditionTarget Target = ConditionTarget.Mean;
        public ConditionComparator Comparator = ConditionComparator.Inferior;
        public float Value = 0.0f;
        #endregion

        #region Public Methods
        public bool Check(Site site)
        {
            float target = 0f;
            switch (Target)
            {
                case ConditionTarget.Mean:
                    target = site.Data.NormalizedValues.Mean();
                    break;
                case ConditionTarget.Median:
                    target = site.Data.NormalizedValues.Median();
                    break;
                case ConditionTarget.StandardDeviation:
                    target = site.Data.NormalizedValues.StandardDeviation();
                    break;
                case ConditionTarget.Max:
                    target = site.Data.NormalizedValues.Max();
                    break;
                case ConditionTarget.Min:
                    target = site.Data.NormalizedValues.Min();
                    break;
            }
            switch (Comparator)
            {
                case ConditionComparator.Superior:
                    if (target > Value) return true;
                    break;
                case ConditionComparator.Inferior:
                    if (target < Value) return true;
                    break;
            }
            return false;
        }
        #endregion
    }
}