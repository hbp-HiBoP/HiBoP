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
    public class MicromedDataInfo : DataInfo
    {
        #region Properties
        const string MICROMED_EXTENSION = ".TRC";

        [DataMember(Name = "TRC")] string m_TRC;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string TRC
        {
            get { return m_TRC?.ConvertToFullPath(); }
            set { m_TRC = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedTRC { get { return m_TRC; } }

        public override string DataFilesString
        {
            get
            {
                return string.Format("{0}", TRC);
            }
        }
        public override string DataTypeString
        {
            get
            {
                return "Micromed";
            }
        }
        public override Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                return Tools.CSharp.EEG.File.FileType.Micromed;
            }
        }
        #endregion

        #region Public Methods
        public override ErrorType[] GetDataErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(TRC))
            {
                errors.Add(ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(TRC);
                if (!headerFile.Exists)
                {
                    errors.Add(ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != MICROMED_EXTENSION)
                    {
                        errors.Add(ErrorType.WrongExtension);
                    }
                    else
                    {
                        Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.Micromed, false, TRC);
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
            m_TRC = TRC.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="trc">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public MicromedDataInfo(string name, Patient patient, NormalizationType normalization, string trc, string id)
        {
            Name = name;
            Patient = patient;
            Normalization = normalization;
            TRC = trc;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public MicromedDataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, string.Empty, Guid.NewGuid().ToString())
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
            return new MicromedDataInfo(Name, Patient, Normalization, TRC, ID);
        }
        public override void Copy(object copy)
        {
            MicromedDataInfo dataInfo = copy as MicromedDataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Normalization = dataInfo.Normalization;
            TRC = dataInfo.TRC;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            m_TRC = m_TRC.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}