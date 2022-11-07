using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

namespace HBP.Core.Data
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
        
        [DataMember] public MEGConfiguration MEGConfiguration { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [IgnoreDataMember] public Processed.MEGData Data { get; set; } = new Processed.MEGData();
        #endregion

        #region Constructors
        public MEGColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, MEGConfiguration fmriConfiguration, string ID) : base(name, baseConfiguration, ID)
        {
            MEGConfiguration = fmriConfiguration;
            Dataset = dataset;
        }
        public MEGColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, MEGConfiguration fmriConfiguration) : base(name, baseConfiguration)
        {
            MEGConfiguration = fmriConfiguration;
            Dataset = dataset;
        }
        public MEGColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, new MEGConfiguration())
        {
        }
        public MEGColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            MEGConfiguration.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(MEGConfiguration.GetAllIdentifiable());
            return IDs;
        }
        public override object Clone()
        {
            return new MEGColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, Dataset, MEGConfiguration.Clone() as MEGConfiguration, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is MEGColumn megColumn)
            {
                MEGConfiguration = megColumn.MEGConfiguration;
                Dataset = megColumn.Dataset;
            }
        }
        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            PatientDataInfo[] megDataInfos = Dataset?.GetMEGDataInfos();
            return Dataset != null && Dataset.Protocol != null && patients.All((patient) => megDataInfos.Any((data) => data.Patient == patient && data.IsOk));
        }
        public override void Unload()
        {
            Data.Unload();
        }
        #endregion  
    }
}