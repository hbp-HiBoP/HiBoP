namespace HBP.Data.Visualization
{
    /**
    * \class Event
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief TimeLine Event.
    * 
    * \details Event of the timeLine which contains:
    *   - \a Label.
    *   - \a Position.
    */
    public class Event
    {
        #region Properties
        /// <summary>
        /// Event label.
        /// </summary>
        string label;
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// Event position in the timeline.
        /// </summary>
        int position;
        public int Position
        {
            get { return position; }
            set { position = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Event instance.
        /// </summary>
        /// <param name="label">Event label.</param>
        /// <param name="position">Event position.</param>
        public Event(string label, int position)
        {
            this.label = label;
            this.position = position;
        }
        /// <summary>
        /// Create a new Event instance with default values.
        /// </summary>
        public Event() : this(string.Empty,0)
        {
        }
        #endregion
    }
}
