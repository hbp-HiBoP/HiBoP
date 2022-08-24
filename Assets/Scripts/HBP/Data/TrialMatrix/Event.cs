namespace HBP.Data.Informations.TrialMatrix
{
    public class Event
    {
        #region Properties
        public int Position { get; set; }
        public Core.Data.Event ProtocolEvent { get; set; }
        #endregion

        #region Constructor
        public Event(int position, Core.Data.Event protocolEvent)
        {
            Position = position;
            ProtocolEvent = protocolEvent;
        }
        #endregion
    }
}

