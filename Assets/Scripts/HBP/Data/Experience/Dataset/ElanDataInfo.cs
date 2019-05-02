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
    public class ElanDataInfo : DataInfo
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
            set { m_POS = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedPOS { get { return m_POS; } }

        [DataMember(Name = "Notes")] string m_Notes;
        /// <summary>
        /// Path of the POS file.
        /// </summary>
        public string Notes
        {
            get { return m_Notes?.ConvertToFullPath(); }
            set { m_Notes = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedNotes { get { return m_Notes; } }

        public override string DataFilesString
        {
            get
            {
                return string.Format("{0};{1};{2}", EEG, POS, Notes);
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
        public override ErrorType[] GetDataErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(EEG))
            {
                errors.Add(ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo EEGFile = new FileInfo(EEG);
                if (!EEGFile.Exists)
                {
                    errors.Add(ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (EEGFile.Extension != EEG_EXTENSION)
                    {
                        errors.Add(ErrorType.WrongExtension);
                    }
                    else
                    {
                        if (!File.Exists(EEGHeader))
                        {
                            errors.Add(ErrorType.RequiredFieldEmpty);
                        }
                        else
                        {
                            if (!(new FileInfo(EEGHeader).Length > 0))
                            {
                                errors.Add(ErrorType.NotEnoughInformation);
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(POS)) errors.Add(ErrorType.RequiredFieldEmpty);
            else
            {
                FileInfo POSFile = new FileInfo(POS);
                if (!POSFile.Exists) errors.Add(ErrorType.FileDoesNotExist);
                else
                {
                    if (POSFile.Extension != POS_EXTENSION) errors.Add(ErrorType.WrongExtension);
                    else
                    {
                        Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.ELAN, false, EEG, POS);
                        List<Tools.CSharp.EEG.Trigger> triggers = file.Triggers;
                        if (!protocol.Blocs.All(bloc => bloc.MainSubBloc.MainEvent.Codes.Any(code => triggers.Any(t => t.Code == code)))) errors.Add(ErrorType.BlocsCantBeEpoched);
                    }
                }
            }
            m_DataErrors = errors.ToArray();
            return m_DataErrors;
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
        public ElanDataInfo(string name, Patient patient, NormalizationType normalization, string eeg, string pos, string notes, string id)
        {
            Name = name;
            Patient = patient;
            Normalization = normalization;
            EEG = eeg;
            POS = pos;
            Notes = notes;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public ElanDataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, string.Empty, string.Empty, string.Empty, Guid.NewGuid().ToString())
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
            return new ElanDataInfo(Name, Patient, Normalization, EEG, POS, Notes, ID);
        }
        public override void Copy(object copy)
        {
            ElanDataInfo dataInfo = copy as ElanDataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Normalization = dataInfo.Normalization;
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