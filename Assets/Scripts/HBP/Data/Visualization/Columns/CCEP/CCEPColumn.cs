﻿using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System.ComponentModel;

namespace HBP.Data.Visualization
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
    [DataContract, DisplayName("CCEP")]
    public class CCEPColumn : Column
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

        [DataMember(Name = "Bloc")] string blocID;
        /// <summary>
        /// Protocol bloc of the column.
        /// </summary>
        public Bloc Bloc
        {
            get
            {
                if (Dataset != null && Dataset.Protocol != null && Dataset.Protocol.Blocs != null)
                {
                    return Dataset.Protocol.Blocs.FirstOrDefault(p => p.ID == blocID);
                }
                else
                {
                    return null;
                }
            }
            set
            {
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
        [DataMember] public DynamicConfiguration DynamicConfiguration { get; set; }

        /// <summary>
        /// Data of the column.
        /// </summary>
        [IgnoreDataMember] public CCEPData Data { get; set; } = new CCEPData();
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
            foreach (Dataset dataset in ApplicationState.ProjectLoaded.Datasets)
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
        public override bool IsCompatible(IEnumerable<Patient> patients)
        {
            CCEPDataInfo[] ccepDataInfos = Dataset?.GetCCEPDataInfos();
            return Dataset != null && Dataset.Protocol != null && Dataset.Protocol.IsVisualizable && patients.All((patient) => ccepDataInfos.Any((data) => data.Name == DataName && data.Patient == patient && data.IsOk));
        }
        public override void Unload()
        {
            Data.Unload();
        }
        public override void GenerateID()
        {
            base.GenerateID();
            DynamicConfiguration.GenerateID();
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
            if(copy is CCEPColumn ccepColumn)
            {
                Dataset = ccepColumn.Dataset;
                DataName = ccepColumn.DataName;
                Bloc = ccepColumn.Bloc;
                DynamicConfiguration = ccepColumn.DynamicConfiguration;
            }
        }
        #endregion
    }
}