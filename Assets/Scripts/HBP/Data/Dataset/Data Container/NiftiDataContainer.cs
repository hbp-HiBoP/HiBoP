using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using System.Collections.ObjectModel;
using HBP.Errors;
using System.ComponentModel;
using System.Linq;

namespace HBP.Data.Container
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
    [DataContract, DisplayName("Nifti"), FMRI]
    public class Nifti : DataContainer
    {
        #region Properties
        readonly string[]  m_Extension = new string[] { ".nii", ".nii.gz", ".img" };
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
        /// Create a new Nifti data container.
        /// </summary>
        /// <param name="file">Path to the file containing the NIFTI data</param>
        /// <param name="ID">Unique identifier</param>
        public Nifti(string file, string ID) : base(ID)
        {
            File = file;
        }
        /// <summary>
        /// Create a new Nifti data container.
        /// </summary>
        /// <param name="file">Path to the file containing the NIFTI data</param>
        public Nifti(string file) : base()
        {
            File = file;
        }
        /// <summary>
        /// Create a new Nifti data container with default values.
        /// </summary>
        public Nifti() : base()
        {

        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(File))
            {
                errors.Add(new RequieredFieldEmptyError("Nifti file path is empty"));
            }
            else
            {
                FileInfo file = new FileInfo(File);
                if (!file.Exists)
                {
                    errors.Add(new FileDoesNotExistError("Nifti file does not exist"));
                }
                else
                {
                    if (!EXTENSION.Any(e => file.FullName.EndsWith(e)))
                    {
                        errors.Add(new WrongExtensionError("Nifti file has a wrong extension"));
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
        #endregion

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new Nifti(File, ID);
        }
        public override void Copy(object copy)
        {
            Nifti nifti = copy as Nifti;
            File = nifti.File;
            ID = nifti.ID;
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