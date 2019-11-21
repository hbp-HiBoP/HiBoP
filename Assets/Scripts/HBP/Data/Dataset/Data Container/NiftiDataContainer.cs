using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using System.Collections.ObjectModel;
using HBP.Errors;
using System.ComponentModel;

namespace HBP.Data.Container
{
    [DataContract, DisplayName("Nifti"), FMRi]
    public class Nifti : DataContainer
    {
        #region Properties
        readonly string[]  m_Extension = new string[] { ".nii", ".img" };
        public ReadOnlyCollection<string> EXTENSION
        {
            get
            {
                return new ReadOnlyCollection<string>(m_Extension);
            }
        }

        [DataMember] string m_Path;
        public string Path
        {
            get
            {
                return m_Path?.ConvertToFullPath();
            }
            set
            {
                m_Path = value?.ConvertToShortPath();
                GetErrors();
                OnRequestErrorCheck.Invoke();
            }
        }
        #endregion

        #region Constructors
        public Nifti(string path, string id) : base(id)
        {
            Path = path;
        }
        public Nifti(string path) : this(path, Guid.NewGuid().ToString())
        {
        }
        public Nifti() : this("", Guid.NewGuid().ToString())
        {

        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors()
        {
            List<Error> errors = new List<Error>(base.GetErrors());
            if (string.IsNullOrEmpty(Path))
            {
                errors.Add(new RequieredFieldEmptyError("Nifti file path is empty"));
            }
            else
            {
                FileInfo file = new FileInfo(Path);
                if (!file.Exists)
                {
                    errors.Add(new FileDoesNotExistError("Nifti file does not exist"));
                }
                else
                {
                    if (!EXTENSION.Contains(file.Extension))
                    {
                        errors.Add(new WrongExtensionError("Nifti file has a wrong extension"));
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

        #region Operators
        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new Nifti(Path, ID);
        }
        public override void Copy(object copy)
        {
            Nifti nifti = copy as Nifti;
            Path = nifti.Path;
            ID = nifti.ID;
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