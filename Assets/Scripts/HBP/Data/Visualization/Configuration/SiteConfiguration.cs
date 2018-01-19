using UnityEngine;
using System;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    /**
    * \class SiteConfiguration
    * \author Adrien Gannerie
    * \version 1.0
    * \date 28 avril 2017
    * \brief Configuration of a site.
    * 
    * \details SiteConfiguration is a class which define the configuration of a site and contains:
    *   - \a Unique ID.
    *   - \a IsMasked.
    *   - \a IsExcluded.
    *   - \a IsBlackListed.
    *   - \a IsHighligted.
    *   - \a IsMarked
    *   - \a Color.
    */
    [DataContract]
    public class SiteConfiguration : ICloneable
    {
        #region Properties
        /// <summary>
        /// The site is excluded ?
        /// </summary>
        [DataMember] public bool IsExcluded { get; set; }

        /// <summary>
        /// The site is blacklisted ?
        /// </summary>
        [DataMember] public bool IsBlacklisted { get; set; }

        /// <summary>
        /// The site is highlighted ?
        /// </summary>
        [DataMember] public bool IsHighlighted { get; set; }

        /// <summary>
        /// The site is marked ?
        /// </summary>
        [DataMember] public bool IsMarked { get; set; }

        [IgnoreDataMember] public float[] Values { get; set; }
        [IgnoreDataMember] public float[] NormalizedValues { get; set; }
        [IgnoreDataMember] public string Unit { get; set; }
        #endregion

        #region Constructors
        public SiteConfiguration(float[] values,float[] normalizedValues,string unit, bool isExcluded, bool isBlacklisted, bool isHighlighted, bool isMarked)
        {
            Values = values;
            NormalizedValues = normalizedValues;
            Unit = unit;
            IsExcluded = isExcluded;
            IsBlacklisted = isBlacklisted;
            IsHighlighted = isHighlighted;
            IsMarked = isMarked;
        }
        public SiteConfiguration(Color color) : this(new float[0],new float[0],"", false, false, false, false) { }
        public SiteConfiguration() : this(new Color()) { }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new SiteConfiguration(Values, NormalizedValues, Unit, IsExcluded, IsBlacklisted, IsHighlighted, IsMarked);
        }
        public void LoadSerializedConfiguration(SiteConfiguration configuration)
        {
            IsBlacklisted = configuration.IsBlacklisted;
            IsExcluded = configuration.IsExcluded;
            IsHighlighted = configuration.IsHighlighted;
            IsMarked = configuration.IsMarked;
        }
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