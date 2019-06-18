using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class EdfDataContainer : DataContainer
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
            set { m_EDF = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedEDF { get { return m_EDF; } }

        public override string[] DataFilesPaths
        {
            get
            {
                return new string[] { EDF };
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
        public override DataInfo.ErrorType[] GetErrors()
        {
            List<DataInfo.ErrorType> errors = new List<DataInfo.ErrorType>(base.GetErrors());
            if (string.IsNullOrEmpty(EDF))
            {
                errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(EDF);
                if (!headerFile.Exists)
                {
                    errors.Add(DataInfo.ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != EDF_EXTENSION)
                    {
                        errors.Add(DataInfo.ErrorType.WrongExtension);
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
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
        public EdfDataContainer(string edf, string id)
        {
            EDF = edf;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public EdfDataContainer() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new EdfDataContainer(EDF, ID);
        }
        public override void Copy(object copy)
        {
            EdfDataContainer dataInfo = copy as EdfDataContainer;
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