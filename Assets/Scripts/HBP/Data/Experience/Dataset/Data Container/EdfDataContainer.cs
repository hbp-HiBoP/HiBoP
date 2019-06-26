using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;

namespace HBP.Data.Container
{
    [DataContract, DisplayName("EDF"), iEEG, CCEP]
    public class EDF : DataContainer
    {
        #region Properties
        const string EDF_EXTENSION = ".edf";

        [DataMember(Name = "EDF")] string m_Path;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string Path
        {
            get { return m_Path?.ConvertToFullPath(); }
            set { m_Path = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedEDF { get { return m_Path; } }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>(base.GetErrors());
            if (string.IsNullOrEmpty(Path))
            {
                errors.Add(new RequieredFieldEmptyError("EDF file path is empty"));
            }
            else
            {
                FileInfo headerFile = new FileInfo(Path);
                if (!headerFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("EDF file does not exist"));
                }
                else
                {
                    if (headerFile.Extension != EDF_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("EDF file has a wrong extension"));
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo dataInfoDirectory, string projectDirectory, string oldProjectDirectory)
        {
            m_Path = Path.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
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
        public EDF(string edf, string id)
        {
            Path = edf;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public EDF() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new EDF(Path, ID);
        }
        public override void Copy(object copy)
        {
            EDF dataInfo = copy as EDF;
            Path = dataInfo.Path;
            ID = dataInfo.ID;
        }
        #endregion

        #region Serialization
        public override void OnDeserializedOperation(StreamingContext context)
        {
            m_Path = m_Path.ToPath();
            base.OnDeserializedOperation(context);
        }
        #endregion
    }
}