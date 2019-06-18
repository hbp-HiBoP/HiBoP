using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Tools.Unity;

namespace HBP.Data.Experience.Dataset
{
    [DataContract]
    public class BrainVisionDataContainer : DataContainer
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

        public override string[] DataFilesPaths
        {
            get
            {
                return new string[] { Header };
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
        public override DataInfo.ErrorType[] GetErrors()
        {
            List<DataInfo.ErrorType> errors = new List<DataInfo.ErrorType>(base.GetErrors());
            if (string.IsNullOrEmpty(Header))
            {
                errors.Add(DataInfo.ErrorType.RequiredFieldEmpty);
            }
            else
            {
                FileInfo headerFile = new FileInfo(Header);
                if (!headerFile.Exists)
                {
                    errors.Add(DataInfo.ErrorType.FileDoesNotExist);
                }
                else
                {
                    if (headerFile.Extension != HEADER_EXTENSION)
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
            // TODO
        }
        #endregion

        #region Constructors
        public BrainVisionDataContainer(string header, string id)
        {
            Header = header;
            ID = id;
        }
        public BrainVisionDataContainer() : this(string.Empty, Guid.NewGuid().ToString())
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
            return new BrainVisionDataContainer(Header, ID);
        }
        public override void Copy(object copy)
        {
            BrainVisionDataContainer dataInfo = copy as BrainVisionDataContainer;
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