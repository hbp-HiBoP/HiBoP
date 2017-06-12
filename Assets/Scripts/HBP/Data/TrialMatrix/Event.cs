namespace HBP.Data.TrialMatrix
{
    public class Event
    {
        #region Properties
        public int Position { get; set; }
        public Experience.Protocol.Event ProtocolEvent { get; set; }
        #endregion

        #region Constructor
        public Event(int position, Experience.Protocol.Event protocolEvent)
        {
            Position = position;
            ProtocolEvent = protocolEvent;
        }
        #endregion
    }
}

