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
    public class MicromedDataContainer : DataContainer
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
            set { m_TRC = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedTRC { get { return m_TRC; } }

        public override string[] DataFilesPaths
        {
            get
            {
                return new string[] { TRC };
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
        public override DataInfo.ErrorType[] GetErrors()
        {
            List<DataInfo.ErrorType> errors = new List<DataInfo.ErrorType>();
            if (string.IsNullOrEmpty(TRC))
            {
                errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(TRC);
                if (!headerFile.Exists)
                {
                    errors.Add(DataInfo.ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != MICROMED_EXTENSION)
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
        public MicromedDataContainer(string trc, string id)
        {
            TRC = trc;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public MicromedDataContainer() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new MicromedDataContainer(TRC, ID);
        }
        public override void Copy(object copy)
        {
            MicromedDataContainer dataInfo = copy as MicromedDataContainer;
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