using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;
using Tools.CSharp;

namespace HBP.Data.Container
{
    [DataContract, DisplayName("Micromed"), iEEG, CCEP]
    public class Micromed : DataContainer
    {
        #region Properties
        const string MICROMED_EXTENSION = ".TRC";

        [DataMember(Name = "TRC")] string m_Path;
        /// <summary>
        /// Path of the EEG file.
        /// </summary>
        public string Path
        {
            get { return m_Path?.ConvertToFullPath(); }
            set { m_Path = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedPath { get { return m_Path; } }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>();
            if (string.IsNullOrEmpty(Path))
            {
                errors.Add(new RequieredFieldEmptyError("TRC file path is empty"));
            }
            else
            {
                FileInfo headerFile = new FileInfo(Path);
                if (!headerFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("TRC file does not exist"));
                }
                else
                {
                    if (headerFile.Extension != MICROMED_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("TRC file has a wrong extension"));
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
        /// <param name="trc">EEG file path.</param>
        /// <param name="pos">POS file path.</param>
        public Micromed(string trc, string id)
        {
            Path = trc;
            ID = id;
        }
        /// <summary>
        /// Create a new DataInfo instance with default value.
        /// </summary>
        public Micromed() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new Micromed(Path, ID);
        }
        public override void Copy(object copy)
        {
            Micromed dataInfo = copy as Micromed;
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