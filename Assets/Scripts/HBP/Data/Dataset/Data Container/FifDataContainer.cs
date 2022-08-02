using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;
using Tools.CSharp;

namespace HBP.Core.Data.Container
{
    /// <summary>
    /// Class which contains IEEG or MEG data in the FIF data format.
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
    /// <description>Path to the FIF file.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("FIF"), IEEG, CCEP, MEGc]
    public class FIF : DataContainer
    {
        #region Properties
        /// <summary>
        /// FIF files extension.
        /// </summary>
        const string FIF_EXTENSION = ".fif";

        /// <summary>
        /// Path to the FIF file with Alias.
        /// </summary>
        [DataMember(Name = "FIF")] public string SavedFile { get; protected set; } = "";
        /// <summary>
        /// Path to the FIF file without Alias.
        /// </summary>
        public string File
        {
            get { return SavedFile?.ConvertToFullPath(); }
            set { SavedFile = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(File))
            {
                errors.Add(new RequieredFieldEmptyError("FIF file path is empty"));
            }
            else
            {
                FileInfo headerFile = new FileInfo(File);
                if (!headerFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("FIF file does not exist"));
                }
                else
                {
                    if (headerFile.Extension != FIF_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("FIF file has a wrong extension"));
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo destinationDirectory, string projectDirectory, string oldProjectDirectory)
        {
            SavedFile = File.CopyToDirectory(destinationDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        public override void ConvertAllPathsToFullPaths()
        {
            SavedFile = SavedFile.ConvertToFullPath();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new FIF data container.
        /// </summary>
        /// <param name="file">Path to the FIF file</param>
        /// <param name="ID"></param>
        public FIF(string file, string ID) : base(ID)
        {
            File = file;
        }
        /// <summary>
        /// Create a new FIF data container.
        /// </summary>
        /// <param name="file">Path to the FIF file</param>
        public FIF(string file) : base()
        {
            File = file;
        }
        /// <summary>
        /// Create a new FIF data container.
        /// </summary>
        public FIF() : this("")
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
            return new FIF(File, ID);
        }
        public override void Copy(object copy)
        {
            FIF dataInfo = copy as FIF;
            File = dataInfo.File;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            SavedFile = SavedFile.StandardizeToEnvironement();
            base.OnDeserialized();
        }
        #endregion
    }
}