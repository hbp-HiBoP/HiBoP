using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Data.Visualization
{
    [DataContract]
    public class SiteData
    {
        #region Properties
        public float[] Values { get; set; }
        public float[] NormalizedValues { get; set; }
        public string Unit { get; set; }
        public int Frequency { get; set; }
        #endregion

        #region constructors
        public SiteData(IEnumerable<float> values, IEnumerable<float> normalizedValues, string unit, int frequency)
        {
            Values = values.ToArray();
            NormalizedValues = normalizedValues.ToArray();
            Unit = unit;
            Frequency = frequency;
        }
        #endregion

        #region Public Methods
        public void Resize(int diffBefore, int diffAfter)
        {
            if (Values.Length == 0) return;

            float[] values = new float[Values.Length + diffBefore + diffAfter];
            float[] normalizedValues = new float[NormalizedValues.Length + diffBefore + diffAfter];
            for (int i = 0; i < diffBefore; ++i)
            {
                values[i] = Values[0];
                normalizedValues[i] = NormalizedValues[0];
            }
            for (int i = 0; i < Values.Length; ++i)
            {
                values[diffBefore + i] = Values[i];
            }
            for (int i = 0; i < NormalizedValues.Length; ++i)
            {
                normalizedValues[diffBefore + i] = NormalizedValues[i];
            }
            for (int i = 0; i < diffAfter; ++i)
            {
                values[diffBefore + Values.Length + i] = Values[Values.Length - 1];
                normalizedValues[diffBefore + NormalizedValues.Length + i] = NormalizedValues[Values.Length - 1];
            }
            Values = values;
            NormalizedValues = normalizedValues;
        }
        /// <summary>
        /// Resize the values array using homemade "interpolation"
        /// </summary>
        /// <param name="size"></param>
        public void ResizeValues(int size, int before, int after)
        {
            if (size == Values.Length || Values.Length == 0) return;

            int length = Values.Length;
            float[] values = new float[size];
            float[] normalizedValues = new float[size];

            for (int i = 0; i < before; ++i)
            {
                values[i] = Values[0];
                normalizedValues[i] = NormalizedValues[0];
            }
            for (int i = before; i < size - after; ++i)
            {
                float floatIndex = ((float)(i - before) / (size - after - 1)) * (length - 1);
                int lowIndex = Mathf.FloorToInt(floatIndex);
                int highIndex = Mathf.CeilToInt(floatIndex);
                float percentage = highIndex - floatIndex;
                values[i] = percentage * Values[lowIndex] + (1 - percentage) * Values[highIndex];
                normalizedValues[i] = percentage * NormalizedValues[lowIndex] + (1 - percentage) * NormalizedValues[highIndex];
            }
            for (int i = size - after; i < size; ++i)
            {
                values[i] = Values[Values.Length - 1];
                normalizedValues[i] = NormalizedValues[NormalizedValues.Length - 1];
            }
            Values = values;
            NormalizedValues = normalizedValues;
        }
        #endregion
    }
}