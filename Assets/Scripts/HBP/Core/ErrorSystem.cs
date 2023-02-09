namespace HBP.Core.Errors
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
    /// <summary>
    /// Error raised when a required field is empty.
    /// </summary>
    public class RequieredFieldEmptyError : Error
    {
        #region Constructors
        public RequieredFieldEmptyError() : this("")
        {

        }
        public RequieredFieldEmptyError(string informations) : base("One of the required fields is empty", informations)
        {

        }
        #endregion
    }
    /// <summary>
    /// Error raised when a specified file doesn't exist.
    /// </summary>
    public class FileDoesNotExistError : Error
    {
        #region Constructors
        public FileDoesNotExistError() : this("")
        {
        }
        public FileDoesNotExistError(string informations) : base("One of the files does not exist", informations)
        {

        }
        #endregion
    }
    /// <summary>
    /// Error raised when a specified file has a wrong extension.
    /// </summary>
    public class WrongExtensionError : Error
    {
        #region Constructors
        public WrongExtensionError() : this("")
        {
        }
        public WrongExtensionError(string informations) : base("One of the files has a wrong extension", informations)
        {

        }
        #endregion
    }
    /// <summary>
    /// Error raised there is not enough informations.
    /// </summary>
    public class NotEnoughInformationError : Error
    {
        #region Constructors
        public NotEnoughInformationError() : this("")
        {
        }
        public NotEnoughInformationError(string informations) : base("One of the files does not contain enough information", informations)
        {

        }
        #endregion
    }
    public class ChannelNotFoundError : Error
    {
        #region Constructors
        public ChannelNotFoundError() : this("")
        {

        }
        public ChannelNotFoundError(string message) : base("The specified stimulated channel could not be found in the patient", message)
        {

        }
        #endregion
    }
    public class LabelEmptyError : Error
    {
        #region Properties
        public LabelEmptyError() : this("")
        {
        }
        public LabelEmptyError(string message) : base("The label field is empty", message)
        {
        }
        #endregion
    }
    public class BlocsCantBeEpochedError : Error
    {
        #region Constructors
        public BlocsCantBeEpochedError() : this("")
        {

        }
        public BlocsCantBeEpochedError(string message) : base("At least one of the blocs of the protocol can't be epoched", message)
        {

        }
        #endregion
    }
    public class PatientEmptyError : Error
    {
        #region Properties
        public PatientEmptyError() : this("")
        {

        }
        public PatientEmptyError(string message) : base("The patient field is empty.", message)
        {

        }
        #endregion
    }


    public abstract class Warning
    {
        #region Properties
        public virtual string Title { get; }
        public virtual string Message { get; }
        #endregion

        #region Constructors
        public Warning() : this("Warning", "Unkwown")
        {

        }
        public Warning(string title, string message)
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
    public class NoMatchingSiteWarning : Warning
    {
        #region Constructors
        public NoMatchingSiteWarning() : this("")
        {

        }
        public NoMatchingSiteWarning(string message) : base("No channel of this data has a name matching a site name of the corresponding patient.", message)
        {

        }
        #endregion
    }
    public class BlocsCantBeEpochedWarning : Warning
    {
        #region Constructors
        public BlocsCantBeEpochedWarning() : this("")
        {

        }
        public BlocsCantBeEpochedWarning(string message) : base("At least one of the blocs of the protocol can't be epoched", message)
        {

        }
        #endregion
    }
}