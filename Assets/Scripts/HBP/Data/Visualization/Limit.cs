namespace HBP.Data.Visualization
{
    /**
    * \class Limit
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief Limit of the timeLine.
    * 
    * \details Limit is a class which represent the limits of a timeline and contains:
    *   - \a Value.
    *   - \a Rounded Value.
    *   - \a Unite.
    *   - \a Position.
    */
    public class Limit
    {
        #region Properties
        float value;
        /// <summary>
        /// Real value of the (min/max) real point.
        /// </summary>
        public float Value { get { return value; } private set { this.value = value; } }

        float rounderValue;
        /// <summary>
        /// The rounded value to display at the limit of the timeline.
        /// </summary>
        public float RoundedValue { get { return rounderValue; } private set { rounderValue = value; } }

        string unite;
        /// <summary>
        /// Unite of the limit.
        /// </summary>
        public string Unite { get { return unite; } private set { unite = value; } }

        int position;
        /// <summary>
        /// Position (index) compared to the timeline . 
        /// </summary>
        public int Position { get { return position; } set { position = value; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new limit instance.
        /// </summary>
        /// <param name="roundedValue">Rounded limit value to display in the UI.</param>
        /// <param name="value">Real limit value.</param>
        /// <param name="unite">Unite of the limit value.</param>
        /// <param name="position">Position of the value compared to the timeline.</param>
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