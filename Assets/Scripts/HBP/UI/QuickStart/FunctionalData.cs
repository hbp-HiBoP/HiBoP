using System;
using HBP.Core.Enums;

namespace HBP.UI.QuickStart
{
    public class FunctionalData
    {
        #region Properties
        public Core.Data.IEEGDataInfo DataInfo { get; set; }
        public Core.Data.Container.BrainVision BrainVisionDataContainer { get; set; } = new Core.Data.Container.BrainVision();
        public Core.Data.Container.Micromed MicromedDataContainer { get; set; } = new Core.Data.Container.Micromed();
        public Core.Data.Container.Elan ElanDataContainer { get; set; } = new Core.Data.Container.Elan();
        public Core.Data.Container.EDF EDFDataContainer { get; set; } = new Core.Data.Container.EDF();
        public Core.Data.Container.FIF FIFDataContainer { get; set; } = new Core.Data.Container.FIF();
        #endregion

        #region Constructors
        public FunctionalData(Core.Data.Patient patient, Type type)
        {
            Core.Data.Container.DataContainer container = null;
            if (type == typeof(Core.Data.Container.BrainVision))
            {
                container = BrainVisionDataContainer;
            }
            else if (type == typeof(Core.Data.Container.Micromed))
            {
                container = MicromedDataContainer;
            }
            else if (type == typeof(Core.Data.Container.Elan))
            {
                container = ElanDataContainer;
            }
            else if (type == typeof(Core.Data.Container.EDF))
            {
                container = EDFDataContainer;
            }
            else if (type == typeof(Core.Data.Container.FIF))
            {
                container = FIFDataContainer;
            }
            DataInfo = new Core.Data.IEEGDataInfo("Data", container, patient, NormalizationType.Auto);
        }
        #endregion

        #region Public Methods
        public void ChangeContainer(Type type)
        {
            Core.Data.Container.DataContainer container = null;
            if (type == typeof(Core.Data.Container.BrainVision))
            {
                container = BrainVisionDataContainer;
            }
            else if (type == typeof(Core.Data.Container.Micromed))
            {
                container = MicromedDataContainer;
            }
            else if (type == typeof(Core.Data.Container.Elan))
            {
                container = ElanDataContainer;
            }
            else if (type == typeof(Core.Data.Container.EDF))
            {
                container = EDFDataContainer;
            }
            else if (type == typeof(Core.Data.Container.FIF))
            {
                container = FIFDataContainer;
            }
            DataInfo.DataContainer = container;
        }
        #endregion
    }
}