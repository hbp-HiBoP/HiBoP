using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using HBP.Core.Errors;
using System.ComponentModel;
using HBP.Core.Tools;

namespace HBP.Core.Data.Container
{
    /// <summary>
    /// Class which contains IEEG or CCEP data in the EDF data format.
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
    /// <description>Path to the EDF file.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Elan"), IEEG, CCEP]
    public class Elan : DataContainer
    {
        #region Properties
        /// <summary>
        /// EEG files extension.
        /// </summary>
        const string EEG_EXTENSION = ".eeg";
        /// <summary>
        /// EEG Header files extension.
        /// </summary>
        const string HEADER_EXTENSION = ".ent";
        /// <summary>
        /// POS files extension.
        /// </summary>
        const string POS_EXTENSION = ".pos";
        /// <summary>
        /// Notes files extension.
        /// </summary>
        const string NOTES_EXTENSION = ".txt";

        /// <summary>
        /// Path to the EEG file with Alias.
        /// </summary>
        [DataMember(Name = "EEG")] public string SavedEEG { get; protected set; } = "";
        /// <summary>
        /// Path to the EEG file without Alias.
        /// </summary>
        public string EEG
        {
            get { return SavedEEG?.ConvertToFullPath(); }
            set { SavedEEG = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        /// <summary>
        /// Path to the EEG header file.
        /// </summary>
        public string EEGHeader
        {
            get
            {
                return EEG + HEADER_EXTENSION;
            }
        }

        /// <summary>
        /// Path to the POS file with Alias.
        /// </summary>
        [DataMember(Name = "POS")] public string SavedPOS { get; protected set; } = "";
        /// <summary>
        /// Path of the POS file without Alias.
        /// </summary>
        public string POS
        {
            get { return SavedPOS?.ConvertToFullPath(); }
            set { SavedPOS = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }

        /// <summary>
        /// Path to the notes file with Alias.
        /// </summary>
        [DataMember(Name = "Notes")] public string SavedNotes { get; protected set; } = "";
        /// <summary>
        /// Path of the notes file without Alias.
        /// </summary>
        public string Notes
        {
            get { return SavedNotes?.ConvertToFullPath(); }
            set { SavedNotes = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(EEG))
            {
                errors.Add(new RequieredFieldEmptyError("EEG file path is empty"));
            }
            else
            {
                FileInfo EEGFile = new FileInfo(EEG);
                if (!EEGFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("EEG file does not exist"));
                }
                else
                {
                    if (EEGFile.Extension != EEG_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("EEG file has a wrong extension"));
                    }
                    else
                    {
                        if (!File.Exists(EEGHeader))
                        {
                            errors.Add(new RequieredFieldEmptyError("EEG header file path is empty"));
                        }
                        else
                        {
                            if (!(new FileInfo(EEGHeader).Length > 0))
                            {
                                errors.Add(new NotEnoughInformationError("EEG header file does not contain enough information"));
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(POS))
            {
                errors.Add(new RequieredFieldEmptyError("POS file path is empty"));
            }
            else
            {
                FileInfo POSFile = new FileInfo(POS);
                if (!POSFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("POS file does not exist"));
                }
                else
                {
                    if (POSFile.Extension != POS_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("POS file has a wrong extension"));
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo destinationDirectory, string projectDirectory, string oldProjectDirectory)
        {
            EEGHeader.CopyToDirectory(destinationDirectory);
            SavedEEG = EEG.CopyToDirectory(destinationDirectory).Replace(projectDirectory, oldProjectDirectory);
            SavedPOS = POS.CopyToDirectory(destinationDirectory).Replace(projectDirectory, oldProjectDirectory);
            SavedNotes = Notes.CopyToDirectory(destinationDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        public override void ConvertAllPathsToFullPaths()
        {
            SavedEEG = SavedEEG.ConvertToFullPath();
            SavedPOS = SavedPOS.ConvertToFullPath();
            SavedNotes = SavedNotes.ConvertToFullPath();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Elan data container.
        /// </summary>
        /// <param name="eeg">Path to the EEG file.</param>
        /// <param name="pos">Path to the POS file.</param>
        /// <param name="notes">Path to the notes file.</param>
        /// <param name="ID">Unique identifier.</param>
        public Elan(string eeg, string pos, string notes, string ID) : base(ID)
        {
            EEG = eeg;
            POS = pos;
            Notes = notes;
        }
        /// <summary>
        /// Create a new Elan data container.
        /// </summary>
        /// <param name="eeg">Path to the EEG file.</param>
        /// <param name="pos">Path to the POS file.</param>
        /// <param name="notes">Path to the notes file.</param>
        public Elan(string eeg, string pos, string notes) : base()
        {
            EEG = eeg;
            POS = pos;
            Notes = notes;
        }
        /// <summary>
        /// Create a new Elan data container with default values.
        /// </summary>
        public Elan() : base()
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
            return new Elan(EEG, POS, Notes, ID);
        }
        public override void Copy(object copy)
        {
            Elan dataInfo = copy as Elan;
            EEG = dataInfo.EEG;
            POS = dataInfo.POS;
            Notes = dataInfo.Notes;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            SavedEEG = SavedEEG.StandardizeToEnvironement();
            SavedPOS = SavedPOS.StandardizeToEnvironement();
            SavedNotes = SavedNotes.StandardizeToEnvironement();
            base.OnDeserialized();
        }
        #endregion
    }
}