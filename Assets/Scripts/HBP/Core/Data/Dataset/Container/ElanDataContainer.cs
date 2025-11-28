using HBP.Core.Errors;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine.Scripting;

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
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Elan"), IEEG, CCEP]
    public class Elan : DataContainer
    {
        #region Properties
        /// <summary>
        /// EEG files extension.
        /// </summary>
        public const string EEG_EXTENSION = ".eeg";
        /// <summary>
        /// EEG Header files extension.
        /// </summary>
        public const string HEADER_EXTENSION = ".ent";
        /// <summary>
        /// POS files extension.
        /// </summary>
        public const string POS_EXTENSION = ".pos";
        /// <summary>
        /// Notes files extension.
        /// </summary>
        public const string NOTES_EXTENSION = ".txt";

        /// <summary>
        /// Path to the EEG file with Alias.
        /// </summary>
        [JsonProperty("EEG")] public string SavedEEG { get; protected set; } = "";
        /// <summary>
        /// Path to the EEG file without Alias.
        /// </summary>
        public string EEG
        {
            get { return SavedEEG?.ConvertToFullPath(); }
            set { SavedEEG = value?.ConvertToShortPath(); }
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
        [JsonProperty("POS")] public string SavedPOS { get; protected set; } = "";
        /// <summary>
        /// Path of the POS file without Alias.
        /// </summary>
        public string POS
        {
            get { return SavedPOS?.ConvertToFullPath(); }
            set { SavedPOS = value?.ConvertToShortPath(); }
        }

        /// <summary>
        /// Path to the notes file with Alias.
        /// </summary>
        [JsonProperty("Notes")] public string SavedNotes { get; protected set; } = "";
        /// <summary>
        /// Path of the notes file without Alias.
        /// </summary>
        public string Notes
        {
            get { return SavedNotes?.ConvertToFullPath(); }
            set { SavedNotes = value?.ConvertToShortPath(); }
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(EEG))
            {
                errors.Add(new RequiredFieldEmptyError("EEG file path is empty"));
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
                    if (!string.Equals(EEGFile.Extension, EEG_EXTENSION, System.StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new WrongExtensionError("EEG file has a wrong extension"));
                    }
                    else
                    {
                        if (!File.Exists(EEGHeader))
                        {
                            errors.Add(new RequiredFieldEmptyError("EEG header file path is empty"));
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
                errors.Add(new RequiredFieldEmptyError("POS file path is empty"));
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
                    if (!string.Equals(POSFile.Extension, POS_EXTENSION, System.StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(new WrongExtensionError("POS file has a wrong extension"));
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
        public Elan(string eeg, string pos, string notes, IEnumerable<Error> errors, IEnumerable<Warning> warnings, string ID) : base(errors, warnings, ID)
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
        public Elan(string eeg, string pos, string notes, IEnumerable<Error> errors, IEnumerable<Warning> warnings) : base(errors, warnings)
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
            return new Elan(EEG, POS, Notes, Errors, Warnings, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Elan elan)
            {
                EEG = elan.EEG;
                POS = elan.POS;
                Notes = elan.Notes;
            }
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