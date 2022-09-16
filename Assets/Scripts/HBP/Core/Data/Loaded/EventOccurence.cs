namespace HBP.Core.Data
{
    public struct EventOccurence
    {
        #region Properties
        public int Code { get; set; }
        public int Index { get; set; }
        public float Time { get; set; }
        #endregion

        #region Constructors
        public EventOccurence(int code, int index, float time = -1)
        {
            Code = code;
            Index = index;
            Time = time;
        }
        #endregion
    }
}