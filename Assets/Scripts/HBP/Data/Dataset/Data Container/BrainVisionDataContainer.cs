using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;

namespace HBP.Data.Container
{
    /// <summary>
    /// Class which contains IEEG or CCEP data in the BrainVision data format.
    /// </summary>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>Errors of the dataContainer.</description>
    /// </item>
    /// <item>
    /// <term><b>Header</b></term>
    /// <description>Path to the BrainVision header file.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("BrainVision"), IEEG, CCEP]
    public class BrainVision : DataContainer
    {
        #region Properties
        /// <summary>
        /// Brain vision header extension.
        /// </summary>
        const string HEADER_EXTENSION = ".vhdr";

        /// <summary>
        /// Path to the BrainVision header file with Alias.
        /// </summary>
        [DataMember(Name = "Header")] public string SavedHeader { get; protected set; } = "";
        /// <summary>
        /// Path to the BrainVision format header file without Alias.
        /// </summary>
        public string Header
        {
            get
            {
                return SavedHeader?.ConvertToFullPath();
            }
            set
            {
                SavedHeader = value?.ConvertToShortPath();
                GetErrors();
                OnRequestErrorCheck.Invoke();
            }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(Header))
            {
                errors.Add(new RequieredFieldEmptyError("BrainVision header file path is empty"));
            }
            else
            {
                FileInfo headerFile = new FileInfo(Header);
                if (!headerFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("BrainVision header file does not exist"));
                }
                else
                {
                    if (headerFile.Extension != HEADER_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("BrainVision header file has a wrong extension"));
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo destinationDirectory, string projectDirectory, string oldProjectDirectory)
        {
            // TODO
        }
        public override void ConvertAllPathsToFullPaths()
        {
            SavedHeader = SavedHeader.ConvertToFullPath();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new BrainVision data container.
        /// </summary>
        /// <param name="header">Path to the BrainVision format header file</param>
        /// <param name="ID">Unique identifier</param>
        public BrainVision(string header, string ID) : base(ID)
        {
            Header = header;
        }
        /// <summary>
        /// Create a new BrainVision data container.
        /// </summary>
        /// <param name="header">Path to the BrainVision format header file</param>
        public BrainVision(string header) : base()
        {
            Header = header;
        }
        /// <summary>
        /// Create a new BrainVision data container with default values.
        /// </summary>
        public BrainVision() : base()
        {
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new BrainVision(Header, ID);
        }
        public override void Copy(object copy)
        {
            BrainVision dataInfo = copy as BrainVision;
            Header = dataInfo.Header;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnSerialized();
            SavedHeader = SavedHeader.StandardizeToEnvironement();
        }
        #endregion
    }
}