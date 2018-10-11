using HBP.Data.Localizer;
using UnityEngine;

namespace HBP.Data.Visualization
{
    public class SiteData
    {
        #region Properties
        public string Unit { get; set; }
        public Frequency Frequency { get; set; }
        public float[] Values { get; set; }
        #endregion

        #region constructors
        public SiteData(float[] values, string unit, Frequency frequency)
        {
            Values = values;
            Unit = unit;
            Frequency = frequency;
        }
        public SiteData() : this(new float[0], "",  new Frequency())
        {

        }
        #endregion

        #region Public Methods
        public void Resize(int diffBefore, int diffAfter)
        {
            if (Values.Length == 0) return;
            float[] resizedValues = new float[Values.Length + diffBefore + diffAfter];
            for (int i = 0; i < diffBefore; ++i) resizedValues[i] = Values[0];
            for (int i = 0; i < Values.Length; ++i) resizedValues[diffBefore + i] = Values[i];
            for (int i = 0; i < diffAfter; ++i) resizedValues[diffBefore + Values.Length + i] = Values[Values.Length - 1];
            Values = resizedValues;
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
            }
            for (int i = before; i < size - after; ++i)
            {
                float floatIndex = ((float)(i - before) / (size - after - 1)) * (length - 1);
                int lowIndex = Mathf.FloorToInt(floatIndex);
                int highIndex = Mathf.CeilToInt(floatIndex);
                float percentage = highIndex - floatIndex;
                values[i] = percentage * Values[lowIndex] + (1 - percentage) * Values[highIndex];
            }
            for (int i = size - after; i < size; ++i)
            {
                values[i] = Values[Values.Length - 1];
            }
            Values = values;
        }
        #endregion
    }
}