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
            float[] values = site.Statistics.Trial.AllValues;
            switch (Target)
            {
                case ConditionTarget.Mean:
                    target = values.Mean();
                    break;
                case ConditionTarget.Median:
                    target = values.Median();
                    break;
                case ConditionTarget.StandardDeviation:
                    target = values.StandardDeviation();
                    break;
                case ConditionTarget.Max:
                    target = values.Max();
                    break;
                case ConditionTarget.Min:
                    target = values.Min();
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