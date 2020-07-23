using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.Events;
using HBP.Errors;

namespace HBP.Data.Container
{
    /// <summary>
    /// Class which contains all the informations about a data.
    /// </summary>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>Errors of the dataContainer.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract]
    public abstract class DataContainer : BaseData
    {
        #region Properties
        protected Error[] m_Errors = new Error[0];
        /// <summary>
        /// Errors of the dataContainer.
        /// </summary>
        public virtual Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>();
                errors.AddRange(m_Errors);
                return errors.Distinct().ToArray();
            }
        }

        /// <summary>
        /// True if the dataContainer is OK, False otherwise.
        /// </summary>v 
        public bool IsOk => Errors.Length == 0;

        /// <summary>
        /// Callback executed when error checking is required.
        /// </summary>
        public UnityEvent OnRequestErrorCheck { get; } = new UnityEvent();
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataContainer instance with a specified ID.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public DataContainer(string ID) : base(ID)
        {
        }
        /// <summary>
        /// Create a new DataContainer instance with default values.
        /// </summary>
        public DataContainer() : base()
        {

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Copy all the files to specified directory.
        /// </summary>
        /// <param name="destinationDirectory">Destination directory to copy the data</param>
        /// <param name="projectDirectory">Actual project directory</param>
        /// <param name="oldProjectDirectory">Old project directory</param>
        public abstract void CopyDataToDirectory(DirectoryInfo destinationDirectory, string projectDirectory, string oldProjectDirectory);
        /// <summary>
        /// Get all the dataContainer errors.
        /// </summary>
        /// <returns>DataContainer errors</returns>
        public abstract Error[] GetErrors();
        #endregion

        #region Errors
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
        #endregion
    }
}