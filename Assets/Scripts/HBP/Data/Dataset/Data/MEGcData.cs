using System;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class MEGcData : Data
    {
        #region Properties
        public virtual Dictionary<string, float[]> ValuesByChannel { get; set; }
        public virtual Dictionary<string, string> UnitByChannel { get; set; }
        public virtual Core.Tools.Frequency Frequency { get; set; }
        #endregion

        #region Constructors
        public MEGcData() : this(new Dictionary<string, float[]>(), new Dictionary<string, string>(), new Core.Tools.Frequency())
        {
        }
        public MEGcData(Dictionary<string, float[]> valuesBySite, Dictionary<string, string> unitBySite, Core.Tools.Frequency frequency)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            Frequency = frequency;
        }
        public MEGcData(MEGcDataInfo dataInfo) : this()
        {
            // Read Data.
            Core.DLL.EEG.File.FileType type;
            string[] files;
            if (dataInfo.DataContainer is Container.BrainVision brainVisionDataContainer)
            {
                type = Core.DLL.EEG.File.FileType.BrainVision;
                files = new string[] { brainVisionDataContainer.Header };
            }
            else if (dataInfo.DataContainer is Container.EDF edfDataContainer)
            {
                type = Core.DLL.EEG.File.FileType.EDF;
                files = new string[] { edfDataContainer.File };
            }
            else if (dataInfo.DataContainer is Container.FIF fifDataContainer)
            {
                type = Core.DLL.EEG.File.FileType.FIF;
                files = new string[] { fifDataContainer.File };
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            Core.DLL.EEG.File file = new Core.DLL.EEG.File(type, true, files);
            if (file.getHandle().Handle == IntPtr.Zero)
            {
                throw new Exception("Data file could not be loaded");
            }
            List<Core.DLL.EEG.Electrode> channels = file.Electrodes;
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
            Frequency = new Core.Tools.Frequency(0);
        }
        #endregion
    }
}