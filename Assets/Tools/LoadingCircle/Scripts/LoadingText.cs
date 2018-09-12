public class LoadingText
{
    #region Properties
    public readonly string Prefix;
    public readonly string Message;
    public readonly string Suffix;
    #endregion

    #region Constructors
    public LoadingText(string prefix = "", string message = "", string suffix = "")
    {
        Prefix = prefix;
        Message = message;
        Suffix = suffix;
    }
    #endregion
}
