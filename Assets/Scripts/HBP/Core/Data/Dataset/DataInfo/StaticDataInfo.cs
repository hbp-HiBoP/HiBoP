using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using HBP.Core.Errors;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class containing paths to CCEP data files.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Name</b></term>
    /// <description>Name of the data.</description>
    /// </item>
    /// <item>
    /// <term><b>Patient</b></term>
    /// <description>Patient who has passed the experiment.</description>
    /// </item>
    /// <item>
    /// <term><b>Stimulated channel</b></term>
    /// <description>Stimulated channel.</description>
    /// </item>
    /// <item>
    /// <term><b>Data container</b></term>
    /// <description>Data container containing all the paths to functional data files.</description>
    /// </item>
    /// <item>
    /// <term><b>Dataset</b></term>
    /// <description>Dataset the dataInfo belongs to.</description>
    /// </item>
    /// <item>
    /// <term><b>IsOk</b></term>
    /// <description>True if the dataInfo is visualizable, False otherwise.</description>
    /// </item>
    /// <item>
    /// <term><b>Errors</b></term>
    /// <description>All dataInfo errors.</description>
    /// </item>
    /// <item>
    /// <term><b>OnRequestErrorCheck</b></term>
    /// <description>Callback executed when error checking is required.</description>
    /// </item>
    /// </list>
    /// </remarks>
    [DataContract, DisplayName("Static")]
    public class StaticDataInfo : PatientDataInfo
    {
        #region Properties
        protected Error[] m_StaticErrors = new Error[0];
        public override Error[] Errors
        {
            get
            {
                List<Error> errors = new List<Error>(base.Errors);
                errors.AddRange(m_StaticErrors);
                return errors.Distinct().ToArray();
            }
        }

        protected Warning[] m_StaticWarnings = new Warning[0];
        public override Warning[] Warnings
        {
            get
            {
                List<Warning> warnings = new List<Warning>(base.Warnings);
                warnings.AddRange(m_StaticWarnings);
                return warnings.Distinct().ToArray();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        /// <param name="id">Unique identifier</param>
        public StaticDataInfo(string name, Container.DataContainer dataContainer, Patient patient, string ID) : base(name, dataContainer, patient, ID)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        /// <param name="name">Name of the CCEP dataInfo.</param>
        /// <param name="dataContainer">Data container of the CCEP dataInfo.</param>
        /// <param name="patient">Patient related to the data.</param>
        /// <param name="channel">Stimulated channel.</param>
        public StaticDataInfo(string name, Container.DataContainer dataContainer, Patient patient) : base(name, dataContainer, patient)
        {
        }
        /// <summary>
        /// Create a new CCEPDataInfo instance.
        /// </summary>
        public StaticDataInfo() : this("Data", new Container.CSV(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault())
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
            return new StaticDataInfo(Name, DataContainer.Clone() as Container.DataContainer, Patient, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
        }
        #endregion

        #region Public Methods
        public override Error[] GetErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>(base.GetErrors(protocol));
            errors.AddRange(GetStaticErrors(protocol));
            return errors.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Error[] GetStaticErrors(Protocol protocol)
        {
            List<Error> errors = new List<Error>();
            if (DataContainer is Container.CSV csvDataContainer)
            {
                Regex csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                if (new FileInfo(csvDataContainer.SavedFile).Exists)
                {
                    using (StreamReader sr = new StreamReader(csvDataContainer.SavedFile))
                    {
                        string line = sr.ReadLine();
                        int length = csvParser.Split(line).Length;
                        int lineCount = 1;
                        while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                        {
                            lineCount++;
                            string[] splits = csvParser.Split(line);
                            if (splits.Length != length)
                            {
                                errors.Add(new InvalidDataFileError(string.Format("Line {0} does not contain enough data elements.", lineCount)));
                            }
                            for (int i = 1; i < splits.Length; ++i)
                            {
                                if (!NumberExtension.TryParseFloat(splits[i], out float result))
                                {
                                    errors.Add(new InvalidDataFileError(string.Format("Data at position [{0},{1}] is not a float and can not be parsed.", lineCount, i)));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            m_StaticErrors = errors.ToArray();
            return m_StaticErrors;
        }
        public override Warning[] GetWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>(base.GetWarnings(protocol));
            warnings.AddRange(GetStaticWarnings(protocol));
            return warnings.Distinct().ToArray();
        }
        /// <summary>
        /// Get all dataInfo errors related to CCEP.
        /// </summary>
        /// <param name="protocol"></param>
        /// <returns>CCEP related errors</returns>
        public virtual Warning[] GetStaticWarnings(Protocol protocol)
        {
            List<Warning> warnings = new List<Warning>();
            m_StaticWarnings = warnings.ToArray();
            return m_StaticWarnings;
        }
        #endregion
    }
}