using UnityEngine;

namespace HBP.Data.Localizer
{
    public class Frequency
    {
        #region Properties
        public float RawValue { get; private set; }
        public int Value { get; private set; }
        public const string UNIT = "Hz";
        #endregion

        #region Constructors
        public Frequency() : this(0) { }
        public Frequency(float rawValue)
        {
            RawValue = rawValue;
            Value = Mathf.RoundToInt(rawValue);
        }
        #endregion

        #region Public Methods
        public float ConvertToNumberOfSamples(int milliseconds)
        {
            return ConvertToNumberOfSamples(milliseconds * 0.001f);
        }
        public float ConvertToNumberOfSamples(float seconds)
        {
            return seconds * Value;
        }
        public int ConvertToCeiledNumberOfSamples(int milliseconds)
        {
            return Mathf.CeilToInt(ConvertToNumberOfSamples(milliseconds));
        }
        public int ConvertToCeiledNumberOfSamples(float seconds)
        {
            return Mathf.CeilToInt(ConvertToNumberOfSamples(seconds));
        }
        public int ConvertToFlooredNumberOfSamples(int milliseconds)
        {
            return Mathf.FloorToInt(ConvertToNumberOfSamples(milliseconds));
        }
        public int ConvertToFlooredNumberOfSamples(float seconds)
        {
            return Mathf.FloorToInt(ConvertToNumberOfSamples(seconds));
        }
        public int ConvertToRoundedNumberOfSamples(int milliseconds)
        {
            return Mathf.RoundToInt(ConvertToNumberOfSamples(milliseconds));
        }
        public int ConvertToRoundedNumberOfSamples(float seconds)
        {
            return Mathf.RoundToInt(ConvertToNumberOfSamples(seconds));
        }
        public float ConvertNumberOfSamplesToSeconds(int numberOfSamples)
        {
            return ConvertNumberOfSamplesToSeconds((float)numberOfSamples);
        }
        public int ConvertNumberOfSamplesToMilliseconds(int numberOfSamples)
        {
            return ConvertNumberOfSamplesToMilliseconds((float)numberOfSamples);
        }
        public float ConvertNumberOfSamplesToSeconds(float numberOfSamples)
        {
            return numberOfSamples / Value;
        }
        public int ConvertNumberOfSamplesToMilliseconds(float numberOfSamples)
        {
            return Mathf.RoundToInt(1000 * ConvertToNumberOfSamples(numberOfSamples));
        }
        #endregion
    }
}