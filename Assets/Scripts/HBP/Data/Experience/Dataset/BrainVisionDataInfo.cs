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
    public class BrainVisionDataInfo : DataInfo
    {
        #region Properties
        const string HEADER_EXTENSION = ".vhdr";

        [DataMember(Name = "Header")] string m_Header;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string Header
        {
            get { return m_Header?.ConvertToFullPath(); }
            set { m_Header = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedHeader { get { return m_Header; } }

        public override string DataFilesString
        {
            get
            {
                return string.Format("{0}", Header);
            }
        }
        public override string DataTypeString
        {
            get
            {
                return "BrainVision";
            }
        }
        public override Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                return Tools.CSharp.EEG.File.FileType.BrainVision;
            }
        }
        #endregion

        #region Public Methods
        public override ErrorType[] GetDataErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(Header))
            {
                errors.Add(ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(Header);
                if (!headerFile.Exists)
                {
                    errors.Add(ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != HEADER_EXTENSION)
                    {
                        errors.Add(ErrorType.WrongExtension);
                    }
                    else
                    {
                        Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.BrainVision, false, Header);
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
            // TODO
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="header">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public BrainVisionDataInfo(string name, Patient patient, NormalizationType normalization, string header, string id)
        {
            Name = name;
            Patient = patient;
            Normalization = normalization;
            Header = header;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public BrainVisionDataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, string.Empty, Guid.NewGuid().ToString())
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
            return new BrainVisionDataInfo(Name, Patient, Normalization, Header, ID);
        }
        public override void Copy(object copy)
        {
            BrainVisionDataInfo dataInfo = copy as BrainVisionDataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Normalization = dataInfo.Normalization;
            Header = dataInfo.Header;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            m_Header = m_Header.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}