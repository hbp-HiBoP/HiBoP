using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    /**
    * \class Column
    * \author Adrien Gannerie
    * \version 1.0
    * \date 10 janvier 2017
    * \brief Visualization column.
    *
    * \detail Visualization column is a class which contains all the information for the display wanted for a column and contains:
    *   - \a Dataset.
    *   - \a Protocol.
    *   - \a Bloc.
    */
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("CCEP")]
    public class CCEPColumn : Column
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

        /// <summary>
        /// Data name of the column.
        /// </summary>
        [JsonProperty] public string DataName { get; set; }

        [JsonProperty("Bloc")] string blocID;
        Bloc m_Bloc;

        /// <summary>
        /// Protocol bloc of the column.
        /// </summary>
        public Bloc Bloc
        {
            get => m_Bloc;
            set
            {
                m_Bloc = value;
                if (value == null)
                {
                    blocID = string.Empty;
                }
                else
                {
                    blocID = value.ID;
                }
            }
        }

        /// <summary>
        /// Configuration of the column.
        /// </summary>
        [JsonProperty] public DynamicConfiguration DynamicConfiguration { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [JsonIgnore] public Processed.CCEPData Data { get; set; } = new Processed.CCEPData();

        #endregion

        #region Constructors

        public CCEPColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, Bloc bloc, DynamicConfiguration configuration, string ID) : base(name, baseConfiguration, ID)
        {
            Dataset = dataset;
            DataName = dataName;
            Bloc = bloc;
            DynamicConfiguration = configuration;
        }

        public CCEPColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, Bloc bloc, DynamicConfiguration configuration) : base(name, baseConfiguration)
        {
            Dataset = dataset;
            DataName = dataName;
            Bloc = bloc;
            DynamicConfiguration = configuration;
        }

        public CCEPColumn(string name, BaseConfiguration baseConfiguration, IEnumerable<Patient> patients) : this(name, baseConfiguration)
        {
            foreach (Dataset dataset in ApplicationState.LoadedProject.Datasets)
            {
                IEEGDataInfo[] iEEGDataInfos = dataset.GetIEEGDataInfos();
                foreach (var dataName in dataset.Data.Select(data => data.Name).Distinct())
                {
                    if (patients.All((patient) => iEEGDataInfos.Any((data) => (data.Patient == patient && data.Name == dataName))))
                    {
                        Dataset = dataset;
                        DataName = dataName;
                        Bloc = dataset.Protocol.Blocs.First();
                        return;
                    }
                }
            }
        }

        public CCEPColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, string.Empty, null, new DynamicConfiguration())
        {
        }

        public CCEPColumn() : this("New column", new BaseConfiguration(), null, string.Empty, null, new DynamicConfiguration())
        {
        }

        #endregion

        #region Public Methods

        internal void ResolveReferences(LoadingContext context)
        {
            m_Dataset = context.ResolveRequired(context.DatasetById, datasetID, "dataset", $"CCEPColumn '{ID}'");
            m_Bloc = m_Dataset == null ? null : context.ResolveBloc(m_Dataset.Protocol?.ID, blocID, $"CCEPColumn '{ID}'");
        }

        public override void GenerateID()
        {
            base.GenerateID();
            DynamicConfiguration.GenerateID();
        }

        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(DynamicConfiguration.GetAllIdentifiable());
            return IDs;
        }

        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            CCEPDataInfo[] ccepDataInfos = Dataset?.GetCCEPDataInfos();
            return Dataset != null && Dataset.Protocol != null && Dataset.Protocol.IsVisualizable && patients.All((patient) => ccepDataInfos.Any((data) => data.Name == DataName && data.Patient == patient && data.IsOk));
        }

        public override void Unload()
        {
            Data.Unload();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Clone of this instance.</returns>
        public override object Clone()
        {
            return new CCEPColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, Dataset, DataName, Bloc, DynamicConfiguration.Clone() as DynamicConfiguration, ID);
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is CCEPColumn ccepColumn)
            {
                Dataset = ccepColumn.Dataset;
                DataName = ccepColumn.DataName;
                Bloc = ccepColumn.Bloc;
                DynamicConfiguration.Copy(ccepColumn.DynamicConfiguration);
            }
        }

        #endregion
    }
}
