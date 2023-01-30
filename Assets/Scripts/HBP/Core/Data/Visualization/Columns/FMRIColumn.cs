﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    [DataContract, DisplayName("FMRI")]
    public class FMRIColumn : Column
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
        /// Data of the column.
        /// </summary>
        [IgnoreDataMember] public Processed.FMRIData Data { get; set; } = new Processed.FMRIData();
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
        public FMRIColumn(string name, BaseConfiguration baseConfiguration) : this(name, baseConfiguration, null, new FMRIConfiguration())
        {
        }
        public FMRIColumn() : this("", new BaseConfiguration())
        {
        }
        #endregion

        #region Public Methods
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
            if(copy is FMRIColumn fmriColumn)
            {
                FMRIConfiguration = fmriColumn.FMRIConfiguration;
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