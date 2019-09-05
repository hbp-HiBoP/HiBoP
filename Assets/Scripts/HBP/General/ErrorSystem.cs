namespace HBP.Errors
{
    public abstract class Error
    {
        #region Properties
        public virtual string Title { get; }
        public virtual string Message { get; }
        #endregion

        #region Constructors
        public Error() : this("Error","Unkwown")
        {

        }
        public Error(string title, string message)
        {
            Title = title;
            Message = message;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return Title + " : " + Message;
        }
        #endregion
    }
}