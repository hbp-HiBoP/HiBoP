using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("MEG")]
    public class MEGColumn : Column
    {
        #region Properties

        [JsonProperty("Dataset")] string datasetID;
        Dataset m_Dataset;

        /// <summary>
        /// Dataset of the column.
        /// </summary>
        public Dataset Dataset
        {
            get => m_Dataset;
            set
            {
                m_Dataset = value;
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

        [JsonProperty] public MEGConfiguration MEGConfiguration { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [JsonIgnore] public Processed.MEGData Data { get; set; } = new Processed.MEGData();

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

        public MEGColumn(string name, BaseConfiguration baseConfiguration, IEnumerable<Patient> patients) : this(name, baseConfiguration)
        {
            foreach (Dataset dataset in ApplicationState.LoadedProject.Datasets)
            {
                PatientDataInfo[] megDataInfos = dataset.GetMEGDataInfos();
                if (patients.All((patient) => megDataInfos.Any((data) => data.Patient == patient)))
                {
                    Dataset = dataset;
                    return;
                }
            }
        }

        public MEGColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, new MEGConfiguration())
        {
        }

        public MEGColumn() : this("", new BaseConfiguration())
        {
        }

        #endregion

        #region Public Methods

        internal void ResolveReferences(LoadingContext context)
        {
            m_Dataset = context.ResolveRequired(context.DatasetById, datasetID, "dataset", $"MEGColumn '{ID}'");
        }

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
            if (copy is MEGColumn megColumn)
            {
                MEGConfiguration.Copy(megColumn.MEGConfiguration);
                Dataset = megColumn.Dataset;
            }
        }

        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            PatientDataInfo[] megDataInfos = Dataset?.GetMEGDataInfos();
            return Dataset != null && Dataset.Protocol != null && patients.Any() && patients.All((patient) => megDataInfos.Any((data) => data.Patient == patient && data.IsOk));
        }

        public override void Unload()
        {
            Data.Unload();
        }

        #endregion
    }
}
