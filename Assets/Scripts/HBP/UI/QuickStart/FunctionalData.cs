using HBP.Data;
using HBP.Data.Experience.Dataset;
using System;

namespace HBP.UI.QuickStart
{
    public class FunctionalData
    {
        #region Properties
        public IEEGDataInfo DataInfo { get; set; }
        public Data.Container.BrainVision BrainVisionDataContainer { get; set; } = new Data.Container.BrainVision();
        public Data.Container.Micromed MicromedDataContainer { get; set; } = new Data.Container.Micromed();
        public Data.Container.Elan ElanDataContainer { get; set; } = new Data.Container.Elan();
        public Data.Container.EDF EDFDataContainer { get; set; } = new Data.Container.EDF();
        public Data.Container.FIF FIFDataContainer { get; set; } = new Data.Container.FIF();
        #endregion

        #region Constructors
        public FunctionalData(Patient patient, Type type)
        {
            Data.Container.DataContainer container = null;
            if (type == typeof(Data.Container.BrainVision))
            {
                container = BrainVisionDataContainer;
            }
            else if (type == typeof(Data.Container.Micromed))
            {
                container = MicromedDataContainer;
            }
            else if (type == typeof(Data.Container.Elan))
            {
                container = ElanDataContainer;
            }
            else if (type == typeof(Data.Container.EDF))
            {
                container = EDFDataContainer;
            }
            else if (type == typeof(Data.Container.FIF))
            {
                container = FIFDataContainer;
            }
            DataInfo = new IEEGDataInfo("Data", container, patient, IEEGDataInfo.NormalizationType.Auto);
        }
        #endregion

        #region Public Methods
        public void ChangeContainer(Type type)
        {
            Data.Container.DataContainer container = null;
            if (type == typeof(Data.Container.BrainVision))
            {
                container = BrainVisionDataContainer;
            }
            else if (type == typeof(Data.Container.Micromed))
            {
                container = MicromedDataContainer;
            }
            else if (type == typeof(Data.Container.Elan))
            {
                container = ElanDataContainer;
            }
            else if (type == typeof(Data.Container.EDF))
            {
                container = EDFDataContainer;
            }
            else if (type == typeof(Data.Container.FIF))
            {
                container = FIFDataContainer;
            }
            DataInfo.DataContainer = container;
        }
        #endregion
    }
}