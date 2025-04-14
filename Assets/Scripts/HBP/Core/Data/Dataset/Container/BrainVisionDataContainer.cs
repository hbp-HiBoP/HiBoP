using System.Collections.Generic;
using System.IO;
using HBP.Core.Errors;
using System.ComponentModel;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Linq;

namespace HBP.Core.Data.Container
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
    [JsonObject(MemberSerialization.OptIn), DisplayName("BrainVision"), IEEG, CCEP, MEGc]
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
        [JsonProperty("Header")] public string SavedHeader { get; protected set; } = "";
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
        public override Warning[] GetWarnings()
        {
            List<Warning> warnings = new List<Warning>();
            m_Warnings = warnings.ToArray();
            return m_Warnings;
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
        public BrainVision(string header, IEnumerable<Error> errors, IEnumerable<Warning> warnings, string ID) : base(errors, warnings, ID)
        {
            Header = header;
        }
        /// <summary>
        /// Create a new BrainVision data container.
        /// </summary>
        /// <param name="header">Path to the BrainVision format header file</param>
        public BrainVision(string header, IEnumerable<Error> errors, IEnumerable<Warning> warnings) : base(errors, warnings)
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
            return new BrainVision(Header, Errors, Warnings, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BrainVision brainVision)
            {
                Header = brainVision.Header;
            }
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