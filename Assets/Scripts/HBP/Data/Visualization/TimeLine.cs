using Tools.CSharp;

namespace HBP.Data.Visualization
{
    /**
    * \class TimeLine
    * \author Adrien Gannerie
    * \version 1.0
    * \date 11 janvier 2017
    * \brief TimeLine of the visusalition which define the limits and the events in the visualization.
    * 
    * \details TimeLine of the visusalition which define the limits and the events in the visualization and contains:
    *   - \a Start limit.
    *   - \a End limit.
    *   - \a Lenght.
    *   - \a Step.
    *   - \a Main \a event.
    *   - \a Secondary \a events.      
    */
    public class Timeline
	{
        #region Properties
        Limit start;
        /// <summary>
        /// Start Limit of the TimeLine wich define the value, the index position and the unite of the limit.
        /// </summary>
        public Limit Start
        {
            get { return start; }
            private set { start = value; }
        }
        Limit end;
        /// <summary>
        /// End Limit of the TimeLine wich define the value, the index position and the unite of the limit.
        /// </summary>
        public Limit End
        {
            get { return end; }
            private set { end = value; }
        }
        int lenght;
        /// <summary>
        /// Number of steps in the TimeLine.
        /// </summary>
        public int Lenght
        {
            get { return lenght; }
            private set { lenght = value; }
        }
        float step;
        /// <summary>
        /// Step in millisecond(ms).
        /// </summary>
        public float Step
        {
            get { return step; }
            private set { step = value; }
        }
        Event[] secondaryEvents;
        /// <summary>
        /// Secondary events which define the label, the code and the index position.
        /// </summary>
        public Event[] SecondaryEvents
        {
            get { return secondaryEvents; }
            private set { secondaryEvents = value; }
        }
        Event mainEvent;
        /// <summary>
        /// Main event which define the label, the code and the index position.
        /// </summary>
        public Event MainEvent
        {
            get { return mainEvent; }
            private set { mainEvent = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new TimeLine instance.
        /// </summary>
        /// <param name="min">Start Limit.</param>
        /// <param name="max">End Limit.</param>
        /// <param name="size">Size of the timeLine.</param>
        /// <param name="mainEvent">Main event.</param>
        /// <param name="secondaryEvents">Secondary events.</param>
        public Timeline(Limit min,Limit max,int size,float step,Event mainEvent, Event[] secondaryEvents)
		{
			Start = min;
			End = max;
			Lenght = size;
            Step = step;
			MainEvent = mainEvent;
			SecondaryEvents = secondaryEvents;
		}		
        /// <summary>
        /// Create a new TimeLine instance.
        /// </summary>
        /// <param name="displayInformations">Display informations.</param>
        /// <param name="mainEvent">Main Event.</param>
        /// <param name="secondaryEvents">Secondary Events.</param>
        /// <param name="frequency">Frequency of the data.</param>
		public Timeline(Window window, Event mainEvent, Event[] secondaryEvents, float frequency)
		{
            int start = UnityEngine.Mathf.CeilToInt(window.Start * 0.001f * frequency);
            int end = UnityEngine.Mathf.FloorToInt(window.End * 0.001f * frequency);
            Lenght = end - start + 1;
            Step = 1000 / frequency;
            Start = new Limit(window.Start, start * Step, "ms",0);
            End = new Limit(window.End, end * Step, "ms",(Lenght-1));
            MainEvent = mainEvent;
            SecondaryEvents = secondaryEvents;
        }
        /// <summary>
        /// Create a new TimeLine instance with default values.
        /// </summary>
        public Timeline() : this(new Window() , new Event(),new Event[0],0.0F)
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resize the timeLine.
        /// </summary>
        /// <param name="diffBefore">Index before.</param>
        /// <param name="diffAfter">Index after.</param>
        public void Resize(int diffBefore, int diffAfter)
        {
            //Resize
            Lenght += diffAfter + diffBefore;
            // Change main event position
            MainEvent.Position += diffBefore;

            // Change secondary Events position
            for (int i = 0; i < SecondaryEvents.Length; i++)
            {
                SecondaryEvents[i].Position += diffBefore;
            }

            // Change Limits
            Start.Position += diffBefore;
            End.Position += diffBefore;
        }
        #endregion
    }
}