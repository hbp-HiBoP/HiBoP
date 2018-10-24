namespace HBP.Data.Experience.Dataset
{
    public class EventInformation
    {
        #region Properties
        public bool IsFound
        {
            get
            {
                return Occurences.Length > 0;
            }
        }
        public bool IsUnique
        {
            get
            {
                return Occurences.Length == 1;
            }
        }
        public EventOccurence[] Occurences { get; private set; }
        #endregion

        #region Constructors
        public EventInformation(EventOccurence[] occurences)
        {
            Occurences = occurences;
        }
        #endregion

        #region Structs
        public struct EventOccurence
        {
            #region Properties
            /// <summary>
            /// Code of the event
            /// </summary>
            public int Code { get; set; }
            /// <summary>
            /// Global index.
            /// </summary>
            public int Index { get; set; }
            /// <summary>
            /// Index of the event relative to the begining of the values.
            /// </summary>
            public int IndexFromStart { get; set; }
            /// <summary>
            /// Index of the event relative to the index of the main event.
            /// </summary>
            public int IndexFromMainEvent { get; set; }
            /// <summary>
            /// Time when the event is called relatively from the beginning of the experience in milliseconds.
            /// </summary>
            public float Time { get; set; }
            /// <summary>
            /// Time of the event relative to the start of the window.
            /// </summary>
            public float TimeFromStart { get; set; }
            /// <summary>
            /// Time of the event relative to the main event.
            /// </summary>
            public float TimeFromMainEvent { get; set; }
            #endregion

            #region Constructors
            public EventOccurence(int code, int index, int indexFromStart, int indexFromMainEvent, float time, float timeFromStart, float timeFromMainEvent)
            {
                Code = code;
                Index = index;
                IndexFromStart = indexFromStart;
                IndexFromMainEvent = indexFromMainEvent;
                Time = time;
                TimeFromStart = timeFromStart;
                TimeFromMainEvent = timeFromMainEvent;
            }
            #endregion
        }
        #endregion
    }
}