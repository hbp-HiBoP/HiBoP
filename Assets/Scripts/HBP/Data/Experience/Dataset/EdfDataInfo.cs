using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class EdfDataInfo : DataInfo
    {
        #region Properties
        const string EDF_EXTENSION = ".edf";

        [DataMember(Name = "EDF")] string m_EDF;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string EDF
        {
            get { return m_EDF?.ConvertToFullPath(); }
            set { m_EDF = value?.ConvertToShortPath(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedEDF { get { return m_EDF; } }

        public override string DataFilesString
        {
            get
            {
                return string.Format("{0}", EDF);
            }
        }
        public override string DataTypeString
        {
            get
            {
                return "EDF";
            }
        }
        public override Tools.CSharp.EEG.File.FileType Type
        {
            get
            {
                return Tools.CSharp.EEG.File.FileType.EDF;
            }
        }
        #endregion

        #region Public Methods
        public override ErrorType[] GetDataErrors(Protocol.Protocol protocol)
        {
            List<ErrorType> errors = new List<ErrorType>();
            if (string.IsNullOrEmpty(EDF))
            {
                errors.Add(ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(EDF);
                if (!headerFile.Exists)
                {
                    errors.Add(ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != EDF_EXTENSION)
                    {
                        errors.Add(ErrorType.WrongExtension);
                    }
                    else
                    {
                        Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.EDF, false, EDF);
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
            m_EDF = EDF.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataInfo instance.
        /// </summary>
        /// <param name="name">Name of the dataInfo.</param>
        /// <param name="patient">Patient who passed the experiment.</param>
        /// <param name="measure">Name of the measure in the EEG file.</param>
        /// <param name="edf">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public EdfDataInfo(string name, Patient patient, NormalizationType normalization, string edf, string id)
        {
            Name = name;
            Patient = patient;
            Normalization = normalization;
            EDF = edf;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public EdfDataInfo() : this("Data", ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), NormalizationType.Auto, string.Empty, Guid.NewGuid().ToString())
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
            return new EdfDataInfo(Name, Patient, Normalization, EDF, ID);
        }
        public override void Copy(object copy)
        {
            EdfDataInfo dataInfo = copy as EdfDataInfo;
            Name = dataInfo.Name;
            Patient = dataInfo.Patient;
            Normalization = dataInfo.Normalization;
            EDF = dataInfo.EDF;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            m_EDF = m_EDF.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}