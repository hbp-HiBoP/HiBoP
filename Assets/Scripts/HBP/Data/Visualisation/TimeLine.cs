namespace HBP.Data.Visualisation
{
	public class TimeLine
	{
        #region Properties
        /// <summary>
        /// Min Limit of the TimeLine wich define the value and the unite of the limit.
        /// </summary>
        Limit m_min;
        public Limit Min { get { return m_min; } private set { m_min = value; } }

        /// <summary>
        /// Max Limit of the TimeLine wich define the value and the unite of the limit.
        /// </summary>
        Limit m_max;
		public Limit Max { get { return m_max; } private set { m_max = value; } }

        /// <summary>
        /// Number of steps in the TimeLine.
        /// </summary>
        int m_size;
		public int Size { get { return m_size; } private set { m_size = value; } }

        /// <summary>
        /// Step in ms;
        /// </summary>
        float m_step;
        public float Step { get { return m_step; } private set { m_step = value; } }

        /// <summary>
        /// Secondary events informations (Label,Position).
        /// </summary>
        Event[] m_secondaryEvents;
		public Event[] SecondaryEvents { get { return m_secondaryEvents; } private set { m_secondaryEvents = value; } }

        /// <summary>
        /// Main event informations (Label,Position).
        /// </summary>
        Event m_mainEvent;
		public Event MainEvent { get { return m_mainEvent; } private set { m_mainEvent = value; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="min">Min Limit.</param>
        /// <param name="max">Max Limit.</param>
        /// <param name="size">Size of the timeLine.</param>
        /// <param name="mainEvent">Main event.</param>
        /// <param name="secondaryEvents">Secondary events.</param>
        public TimeLine(Limit min,Limit max,int size,float step,Event mainEvent, Event[] secondaryEvents)
		{
			Min = min;
			Max = max;
			Size = size;
            Step = step;
			MainEvent = mainEvent;
			SecondaryEvents = secondaryEvents;
		}
		
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="mainEvent">Main Event.</param>
        /// <param name="secondaryEvents">Secondary Events.</param>
        /// <param name="frequency">Frequency of the data.</param>
		public TimeLine(Experience.Protocol.DisplayInformations displayInformations, Event mainEvent, Event[] secondaryEvents, float frequency)
		{
            int l_max = UnityEngine.Mathf.FloorToInt((displayInformations.Window.y) * 0.001f * frequency);
            int l_min = UnityEngine.Mathf.CeilToInt((displayInformations.Window.x) * 0.001f * frequency);
            Size = l_max - l_min;
            Step = 1000 / frequency;
            float l_realMin = l_min * Step;
            float l_realMax = l_max * Step;
            Min = new Limit(displayInformations.Window.x, l_realMin, "ms",0);
            Max = new Limit(displayInformations.Window.y, l_realMax, "ms",(Size-1));
            MainEvent = mainEvent;
            SecondaryEvents = secondaryEvents;
        }

        public TimeLine() : this(new Experience.Protocol.DisplayInformations(),new Event(),new Event[0],0.0F)
        {
        }
        #endregion

        #region Public Methods
        public void Resize(int diffBefore, int diffAfter)
        {
            //Resize
            Size += diffAfter + diffBefore;
            // Change main event position
            MainEvent.Position += diffBefore;

            // Change secondary Events position
            for (int i = 0; i < SecondaryEvents.Length; i++)
            {
                SecondaryEvents[i].Position += diffBefore;
            }

            // Change Limits
            Min.Position += diffBefore;
            Max.Position += diffBefore;
        }
        #endregion
    }
}