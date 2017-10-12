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
    *   - \a Attendance rate.
    */
    public class Event
    {
        #region Properties
        /// <summary>
        /// Event label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Event position in the timeline.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Event attendance rate.
        /// </summary>
        public float AttendanceRate { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Event instance.
        /// </summary>
        /// <param name="label">Event label.</param>
        /// <param name="position">Event position.</param>
        /// <param name="attendanceRate">Event attendance rate.</param>
        public Event(string label, int position, float attendanceRate = 1f)
        {
            Label = label;
            Position = position;
            AttendanceRate = attendanceRate;
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
