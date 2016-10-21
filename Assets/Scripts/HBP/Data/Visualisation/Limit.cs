namespace HBP.Data.Visualisation
{
    /// <summary>
    /// TimeLine limit.
    /// </summary>
    public class Limit
    {
        #region Properties
        /// <summary>
        /// Real value of the (min/max) real point.
        /// </summary>
        float m_value;
        public float Value { get { return m_value; } private set { m_value = value; } }

        /// <summary>
        /// The rounded value to display at the limit of the timeline.
        /// </summary>
        float m_roundedValue;
        public float RoundedValue { get { return m_roundedValue; } private set { m_roundedValue = value; } }

        /// <summary>
        /// Unite of the limit.
        /// </summary>
        string m_unite;
        public string Unite { get { return m_unite; } private set { m_unite = value; } }

        int m_position;
        public int Position { get { return m_position; } set { m_position = value; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value of the limit.</param>
        /// <param name="unite">The unite of the limit.</param>
        /// <param name="position">The position of the limit.</param>
        public Limit(float roundedValue,float value, string unite,int position)
        {
            Value = value;
            RoundedValue = roundedValue;
            Unite = unite;
            Position = position;
        }
        #endregion
    }
}