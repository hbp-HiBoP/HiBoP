using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Tools.Unity;
using UnityEngine;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class ElanDataContainer : DataContainer
    {
        #region Properties
        const string EEG_EXTENSION = ".eeg";
        const string POS_EXTENSION = ".pos";
        const string NOTES_EXTENSION = ".txt";

        [DataMember(Name = "EEG")] string m_EEG;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EEG
        {
            get { return m_EEG?.ConvertToFullPath(); }
            set { m_EEG = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedEEG { get { return m_EEG; } }
        public string EEGHeader
        {
            get
            {
                return EEG + ".ent";
            }
        }

        [DataMember(Name = "POS")] string m_POS;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string POS
        {
            get { return m_POS?.ConvertToFullPath(); }
            set { m_POS = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedPOS { get { return m_POS; } }

        [DataMember(Name = "Notes")] string m_Notes;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string Notes
        {
            get { return m_Notes?.ConvertToFullPath(); }
            set { m_Notes = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedNotes { get { return m_Notes; } }

        public override string[] DataFilesPaths
        {
            get
            {
                return new string[] { EEG, POS, Notes };
            }
        }
        public override string DataTypeString
        {
            get
            {
                return "ELAN";
            }
        }
        public override Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                return Tools.CSharp.EEG.File.FileType.ELAN;
            }
        }
        #endregion

        #region Public Methods
        public override DataInfo.ErrorType[] GetErrors()
        {
            List<DataInfo.ErrorType> errors = new List<DataInfo.ErrorType>(base.GetErrors());
            if (string.IsNullOrEmpty(EEG))
            {
                errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo EEGFile = new FileInfo(EEG);
                if (!EEGFile.Exists)
                {
                    errors.Add(DataInfo.ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (EEGFile.Extension != EEG_EXTENSION)
                    {
                        errors.Add(DataInfo.ErrorType.WrongExtension);
                    }
                    else
                    {
                        if (!File.Exists(EEGHeader))
                        {
                            errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
                        }
                        else
                        {
                            if (!(new FileInfo(EEGHeader).Length > 0))
                            {
                                errors.Add(DataInfo.ErrorType.NotEnoughInformation);
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(POS)) errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
            else
            {
                FileInfo POSFile = new FileInfo(POS);
                if (!POSFile.Exists) errors.Add(DataInfo.ErrorType.FileDoesNotExist);
                else
                {
                    if (POSFile.Extension != POS_EXTENSION) errors.Add(DataInfo.ErrorType.WrongExtension);
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo dataInfoDirectory, string projectDirectory, string oldProjectDirectory)
        {
            EEGHeader.CopyToDirectory(dataInfoDirectory);
            m_EEG = EEG.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
            m_POS = POS.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
            m_Notes = Notes.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="eeg">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public ElanDataContainer(string eeg, string pos, string notes, string id)
        {
            EEG = eeg;
            POS = pos;
            Notes = notes;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public ElanDataContainer() : this(string.Empty, string.Empty, string.Empty, Guid.NewGuid().ToString())
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
            return new ElanDataContainer(EEG, POS, Notes, ID);
        }
        public override void Copy(object copy)
        {
            ElanDataContainer dataInfo = copy as ElanDataContainer;
            EEG = dataInfo.EEG;
            POS = dataInfo.POS;
            Notes = dataInfo.Notes;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            m_EEG = m_EEG.ToPath();
            m_POS = m_POS.ToPath();
            m_Notes = m_Notes.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}