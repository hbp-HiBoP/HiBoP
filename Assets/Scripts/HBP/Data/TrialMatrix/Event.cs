namespace HBP.Data.TrialMatrix
{
    public class Event
    {
        #region Properties
        int position;   
        public int Position { get; set; }

        int code;
        public int Code { get; set; }
        #endregion

        #region Constructor
        public Event(int position, int code)
        {
            Position = position;
            Code = code;
        }

        public Event() : this(0,0)
        {
        }
        #endregion
    }
}

