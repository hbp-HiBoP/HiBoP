using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace HBP.Data.Visualization
{
    [DataContract, DisplayName("MEG")]
    public class MEGColumn : Column
    {
        #region Properties
        [DataMember(Name = "Dataset")] string datasetID;
        /// <summary>
        /// Dataset of the column.
        /// </summary>
        public Dataset Dataset
        {
            get
            {
                return ApplicationState.ProjectLoaded.Datasets.FirstOrDefault(p => p.ID == datasetID);
            }
            set
            {
                if (value == null)
                {
                    datasetID = string.Empty;
                }
                else
                {
                    datasetID = value.ID;
                }
            }
        }
        
        [DataMember] public FMRIConfiguration FMRIConfiguration { get; set; }

        /// <summary>
        /// Data name of the column.
        /// </summary>
        [DataMember] public string DataName { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [IgnoreDataMember] public MEGData Data { get; set; } = new MEGData();
        #endregion

        #region Constructors
        public MEGColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, FMRIConfiguration fmriConfiguration, string ID) : base(name, baseConfiguration, ID)
        {
            FMRIConfiguration = fmriConfiguration;
            Dataset = dataset;
            DataName = dataName;
        }
        public MEGColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, FMRIConfiguration fmriConfiguration) : base(name, baseConfiguration)
        {
            FMRIConfiguration = fmriConfiguration;
            Dataset = dataset;
            DataName = dataName;
        }
        public MEGColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, "", new FMRIConfiguration())
        {
        }
        public MEGColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public override object Clone()
        {
            return new MEGColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, Dataset, DataName, FMRIConfiguration.Clone() as FMRIConfiguration, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is MEGColumn megColumn)
            {
                FMRIConfiguration = megColumn.FMRIConfiguration;
                Dataset = megColumn.Dataset;
                DataName = megColumn.DataName;
            }
        }
        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            return true;
        }
        public override void Unload()
        {
            Data.Unload();
        }
        #endregion  
    }
}