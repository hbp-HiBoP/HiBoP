using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("FMRI")]
    public class FMRIColumn : Column
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

        [JsonProperty] public FMRIConfiguration FMRIConfiguration { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [JsonIgnore] public Processed.FMRIData Data { get; set; } = new Processed.FMRIData();

        #endregion

        #region Constructors

        public FMRIColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, FMRIConfiguration fmriConfiguration, string ID) : base(name, baseConfiguration, ID)
        {
            FMRIConfiguration = fmriConfiguration;
            Dataset = dataset;
        }

        public FMRIColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, FMRIConfiguration fmriConfiguration) : base(name, baseConfiguration)
        {
            FMRIConfiguration = fmriConfiguration;
            Dataset = dataset;
        }

        public FMRIColumn(string name, BaseConfiguration baseConfiguration, IEnumerable<Patient> patients) : this(name, baseConfiguration)
        {
            foreach (Dataset dataset in ApplicationState.LoadedProject.Datasets)
            {
                FMRIDataInfo[] fmriDataInfos = dataset.GetFMRIDataInfos();
                if (patients.All((patient) => fmriDataInfos.Any((data) => data.Patient == patient)))
                {
                    Dataset = dataset;
                    return;
                }
            }
        }

        public FMRIColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, new FMRIConfiguration())
        {
        }

        public FMRIColumn() : this("", new BaseConfiguration())
        {
        }

        #endregion

        #region Public Methods

        internal void ResolveReferences(LoadingContext context)
        {
            m_Dataset = context.ResolveRequired(context.DatasetById, datasetID, "dataset", $"FMRIColumn '{ID}'");
        }

        public override void GenerateID()
        {
            base.GenerateID();
            FMRIConfiguration.GenerateID();
        }

        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(FMRIConfiguration.GetAllIdentifiable());
            return IDs;
        }

        public override object Clone()
        {
            return new FMRIColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, Dataset, FMRIConfiguration.Clone() as FMRIConfiguration, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is FMRIColumn fmriColumn)
            {
                FMRIConfiguration.Copy(fmriColumn.FMRIConfiguration);
                Dataset = fmriColumn.Dataset;
            }
        }

        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            FMRIDataInfo[] fmriDataInfos = Dataset?.GetFMRIDataInfos();
            return Dataset != null && Dataset.Protocol != null && (patients.All((patient) => fmriDataInfos.Any((data) => data.Patient == patient && data.IsOk)) || Dataset?.GetSharedFMRIDataInfos().Length > 0);
        }

        public override void Unload()
        {
            Data.Unload();
        }

        #endregion
    }
}
