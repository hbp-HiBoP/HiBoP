using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Events;
using HBP.Core.Errors;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace HBP.Core.Data.Container
{
    /// <summary>
    /// Class which contains all the informations about a data.
    /// </summary>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>ID</b></term>
    /// <description>Unique identifier of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>Errors of the dataContainer.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DataContainer : BaseData
    {
        #region Properties
        [JsonProperty] protected Error[] m_Errors = new Error[0];
        /// <summary>
        /// Errors of the dataContainer.
        /// </summary>
        public virtual ReadOnlyCollection<Error> Errors => new(m_Errors);

        [JsonProperty] protected Warning[] m_Warnings = new Warning[0];
        /// <summary>
        /// Errors of the dataContainer.
        /// </summary>
        public virtual ReadOnlyCollection<Warning> Warnings => new(m_Warnings);

        /// <summary>
        /// True if the dataContainer is OK, False otherwise.
        /// </summary>v 
        public bool IsOk => Errors.Count == 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new DataContainer instance with a specified ID.
        /// </summary>
        /// <param name="ID">Unique identifier</param>
        public DataContainer(IEnumerable<Error> errors, IEnumerable<Warning> warnings, string ID) : base(ID)
        {
            m_Errors = errors.ToArray();
            m_Warnings = warnings.ToArray();
        }
        public DataContainer(IEnumerable<Error> errors, IEnumerable<Warning> warnings) : base()
        {
            m_Errors = errors.ToArray();
            m_Warnings = warnings.ToArray();
        }
        /// <summary>
        /// Create a new DataContainer instance with default values.
        /// </summary>
        public DataContainer() : base()
        {

        }
        #endregion

        #region Operators
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is DataContainer dataContainer)
            {
                m_Errors = dataContainer.m_Errors;
                m_Warnings = dataContainer.m_Warnings;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Copy all the files to specified directory.
        /// </summary>
        /// <param name="destinationDirectory">Destination directory to copy the data</param>
        /// <param name="projectDirectory">Actual project directory</param>
        /// <param name="oldProjectDirectory">Old project directory</param>
        public abstract void CopyDataToDirectory(DirectoryInfo destinationDirectory, string projectDirectory, string oldProjectDirectory);
        /// <summary>
        /// Get all the dataContainer errors.
        /// </summary>
        /// <returns>DataContainer errors</returns>
        public abstract Error[] GetErrors();
        public abstract Warning[] GetWarnings();

        public abstract void ConvertAllPathsToFullPaths();
        #endregion
    }
}