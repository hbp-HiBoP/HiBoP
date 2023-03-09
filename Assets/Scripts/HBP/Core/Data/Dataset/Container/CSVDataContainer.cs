using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using HBP.Core.Errors;
using System.ComponentModel;
using System.Linq;
using HBP.Core.Tools;

namespace HBP.Core.Data.Container
{
    /// <summary>
    /// Class which contains all the informations about Neuroimaging Informatics Technology Initiative(NIFTI).
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
    /// <term><b>File</b></term>
    /// <description>File containing the NIFTI data.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("CSV"), Static]
    public class CSV : DataContainer
    {
        #region Properties
        readonly string[]  m_Extension = new string[] { ".csv" };
        /// <summary>
        /// Extensions allowed for Nifti data.
        /// </summary>
        public ReadOnlyCollection<string> EXTENSION
        {
            get
            {
                return new ReadOnlyCollection<string>(m_Extension);
            }
        }

        [DataMember(Name = "File")] public string SavedFile { get; protected set; }
        /// <summary>
        /// Path to the file containing the NIFTI data.
        /// </summary>
        public string File
        {
            get
            {
                return SavedFile?.ConvertToFullPath();
            }
            set
            {
                SavedFile = value?.ConvertToShortPath();
                GetErrors();
                OnRequestErrorCheck.Invoke();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new CSV data container.
        /// </summary>
        /// <param name="file">Path to the file containing the CSV data</param>
        /// <param name="ID">Unique identifier</param>
        public CSV(string file, string ID) : base(ID)
        {
            File = file;
        }
        /// <summary>
        /// Create a new Nifti data container.
        /// </summary>
        /// <param name="file">Path to the file containing the NIFTI data</param>
        public CSV(string file) : base()
        {
            File = file;
        }
        /// <summary>
        /// Create a new Nifti data container with default values.
        /// </summary>
        public CSV() : base()
        {

        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(File))
            {
                errors.Add(new RequieredFieldEmptyError("CSV file path is empty"));
            }
            else
            {
                FileInfo file = new FileInfo(File);
                if (!file.Exists)
                {
                    errors.Add(new FileDoesNotExistError("CSV file does not exist"));
                }
                else
                {
                    if (!EXTENSION.Any(e => file.FullName.EndsWith(e)))
                    {
                        errors.Add(new WrongExtensionError("CSV file has a wrong extension"));
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
            SavedFile = SavedFile.ConvertToFullPath();
        }
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new CSV(File, ID);
        }
        public override void Copy(object copy)
        {
            CSV csv = copy as CSV;
            File = csv.File;
            ID = csv.ID;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            SavedFile = SavedFile.StandardizeToEnvironement();
        }
        #endregion
    }
}