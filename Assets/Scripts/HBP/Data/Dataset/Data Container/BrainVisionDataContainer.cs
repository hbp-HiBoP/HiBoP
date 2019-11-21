using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;

namespace HBP.Data.Container
{
    [DataContract, DisplayName("BrainVision"), iEEG, CCEP]
    public class BrainVision : DataContainer
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
            set { m_Header = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
        public string SavedHeader { get { return m_Header; } }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>(base.GetErrors());
            if (string.IsNullOrEmpty(Header))
            {
                errors.Add(new RequieredFieldEmptyError("BrainVision header file path is empty"));
            }
            else
            {
                FileInfo headerFile = new FileInfo(Header);
                if (!headerFile.Exists)
                {
                    errors.Add(new FileDoesNotExistError("BrainVision header file does not exist"));
                }
                else
                {
                    if (headerFile.Extension != HEADER_EXTENSION)
                    {
                        errors.Add(new WrongExtensionError("BrainVision header file has a wrong extension"));
                    }
                }
            }
            m_Errors = errors.ToArray();
            return m_Errors;
        }
        public override void CopyDataToDirectory(DirectoryInfo dataInfoDirectory, string projectDirectory, string oldProjectDirectory)
        {
            // TODO
        }
        #endregion

        #region Constructors
        public BrainVision(string header, string id)
        {
            Header = header;
            ID = id;
        }
        public BrainVision() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new BrainVision(Header, ID);
        }
        public override void Copy(object copy)
        {
            BrainVision dataInfo = copy as BrainVision;
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