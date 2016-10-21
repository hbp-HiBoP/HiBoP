namespace HBP.Data.Visualisation
{
    /// <summary>
    /// Timeline Event.
    /// </summary>
    public class Event
    {
        #region Properties
        /// <summary>
        /// Event label.
        /// </summary>
        string m_label;
        public string Label { get { return m_label; } set { m_label = value; } }

        /// <summary>
        /// Event position in the timeline.
        /// </summary>
        int m_position;
        public int Position
        {
            get { return m_position; }
            set { m_position = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">Event label.</param>
        /// <param name="position">Event position.</param>
        public Event(string label, int position)
        {
            m_label = label;
            m_position = position;
        }

        public Event() : this(string.Empty,0)
        {

        }
        #endregion
    }
}
