using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    [DataContract, DisplayName("Static")]
    public class StaticColumn : Column
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

        /// <summary>
        /// Data name of the column.
        /// </summary>
        [DataMember] public string DataName { get; set; }

        [DataMember] public StaticConfiguration StaticConfiguration { get; set; }
        
        /// <summary>
        /// Data of the column.
        /// </summary>
        [IgnoreDataMember] public Processed.StaticData Data { get; set; } = new Processed.StaticData();
        #endregion

        #region Constructors
        public StaticColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, StaticConfiguration staticConfiguration, string ID) : base(name, baseConfiguration, ID)
        {
            StaticConfiguration = staticConfiguration;
            Dataset = dataset;
            DataName = dataName;
        }
        public StaticColumn(string name, BaseConfiguration baseConfiguration, Dataset dataset, string dataName, StaticConfiguration staticConfiguration) : base(name, baseConfiguration)
        {
            StaticConfiguration = staticConfiguration;
            Dataset = dataset;
            DataName = dataName;
        }
        public StaticColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, string.Empty, new StaticConfiguration())
        {
        }
        public StaticColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
        public override void GenerateID()
        {
            base.GenerateID();
            StaticConfiguration.GenerateID();
        }
        public override List<BaseData> GetAllIdentifiable()
        {
            List<BaseData> IDs = base.GetAllIdentifiable();
            IDs.AddRange(StaticConfiguration.GetAllIdentifiable());
            return IDs;
        }
        public override object Clone()
        {
            return new StaticColumn(Name, BaseConfiguration.Clone() as BaseConfiguration, Dataset, DataName, StaticConfiguration.Clone() as StaticConfiguration, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is StaticColumn staticColumn)
            {
                StaticConfiguration = staticColumn.StaticConfiguration;
                Dataset = staticColumn.Dataset;
                DataName = staticColumn.DataName;
            }
        }
        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            StaticDataInfo[] staticDataInfos = Dataset?.GetStaticDataInfos();
            return Dataset != null && Dataset.Protocol != null && patients.All((patient) => staticDataInfos.Any((data) => data.Patient == patient && data.IsOk));
        }
        public override void Unload()
        {
            Data.Unload();
        }
        #endregion  
    }
}