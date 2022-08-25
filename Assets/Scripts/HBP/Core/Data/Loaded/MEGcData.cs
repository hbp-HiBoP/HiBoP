using System;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class MEGcData : Data
    {
        #region Properties
        public virtual Dictionary<string, float[]> ValuesByChannel { get; set; }
        public virtual Dictionary<string, string> UnitByChannel { get; set; }
        public virtual Tools.Frequency Frequency { get; set; }
        #endregion

        #region Constructors
        public MEGcData() : this(new Dictionary<string, float[]>(), new Dictionary<string, string>(), new Tools.Frequency())
        {
        }
        public MEGcData(Dictionary<string, float[]> valuesBySite, Dictionary<string, string> unitBySite, Tools.Frequency frequency)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            Frequency = frequency;
        }
        public MEGcData(MEGcDataInfo dataInfo) : this()
        {
            // Read Data.
            DLL.EEG.File.FileType type;
            string[] files;
            if (dataInfo.DataContainer is Container.BrainVision brainVisionDataContainer)
            {
                type = DLL.EEG.File.FileType.BrainVision;
                files = new string[] { brainVisionDataContainer.Header };
            }
            else if (dataInfo.DataContainer is Container.EDF edfDataContainer)
            {
                type = DLL.EEG.File.FileType.EDF;
                files = new string[] { edfDataContainer.File };
            }
            else if (dataInfo.DataContainer is Container.FIF fifDataContainer)
            {
                type = DLL.EEG.File.FileType.FIF;
                files = new string[] { fifDataContainer.File };
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            DLL.EEG.File file = new DLL.EEG.File(type, true, files);
            if (file.getHandle().Handle == IntPtr.Zero)
            {
                throw new Exception("Data file could not be loaded");
            }
            List<DLL.EEG.Electrode> channels = file.Electrodes;
            foreach (var channel in channels)
            {
                ValuesByChannel.Add(channel.Label, channel.Data);
                UnitByChannel.Add(channel.Label, channel.Unit);
            }
            Frequency = file.SamplingFrequency;
            file.Dispose();
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            ValuesByChannel.Clear();
            UnitByChannel.Clear();
            Frequency = new Tools.Frequency(0);
        }
        #endregion
    }
}