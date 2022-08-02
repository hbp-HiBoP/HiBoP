using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Tools.Unity;
using HBP.Errors;
using System.ComponentModel;
using Tools.CSharp;

namespace HBP.Core.Data.Container
{
    /// <summary>
    /// Class which contains IEEG or CCEP data in the Micromed data format.
    /// </summary>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>Errors of the dataContainer.</description>
    /// </item>
    /// <item>
    /// <term><b>File</b></term>
    /// <description>File containing the NIFTI data.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Micromed"), IEEG, CCEP]
    public class Micromed : DataContainer
    {
        #region Properties
        const string MICROMED_EXTENSION = ".TRC";

        /// <summary>
        /// Path to the EEG file with Alias.
        /// </summary>
        [DataMember(Name = "TRC")] public string SavedPath { get; protected set; } = "";
        /// <summary>
        /// Path of the EEG file without Alias.
        /// </summary>
        public string Path
        {
            get { return SavedPath?.ConvertToFullPath(); }
            set { SavedPath = value?.ConvertToShortPath(); GetErrors(); OnRequestErrorCheck.Invoke(); }
        }
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
            SavedPath = Path.CopyToDirectory(dataInfoDirectory).Replace(projectDirectory, oldProjectDirectory);
        }
        public override void ConvertAllPathsToFullPaths()
        {
            SavedPath = SavedPath.ConvertToFullPath();
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trc"></param>
        /// <param name="ID"></param>
        public Micromed(string trc, string ID) : base(ID)
        {
            Path = trc;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="trc"></param>
        public Micromed(string trc) : base()
        {
            Path = trc;
        }
        /// <summary>
        /// 
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
            base.Copy(copy);
            if(copy is Micromed micromed)
            {
                Path = micromed.Path;
            }
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            SavedPath = SavedPath.StandardizeToEnvironement();
        }
        #endregion
    }
}