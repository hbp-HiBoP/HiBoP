using HBP.Module3D;
using System;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public class MEGcData : Data
    {
        #region Properties
        public virtual Dictionary<string, float[]> ValuesByChannel { get; set; }
        public virtual Dictionary<string, string> UnitByChannel { get; set; }
        public virtual Tools.CSharp.EEG.Frequency Frequency { get; set; }
        #endregion

        #region Constructors
        public MEGcData() : this(new Dictionary<string, float[]>(), new Dictionary<string, string>(), new Tools.CSharp.EEG.Frequency())
        {
        }
        public MEGcData(Dictionary<string, float[]> valuesBySite, Dictionary<string, string> unitBySite, Tools.CSharp.EEG.Frequency frequency)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            Frequency = frequency;
        }
        public MEGcData(MEGcDataInfo dataInfo) : this()
        {
            // Read Data.
            Tools.CSharp.EEG.File.FileType type;
            string[] files;
            if (dataInfo.DataContainer is Container.BrainVision brainVisionDataContainer)
            {
                type = Tools.CSharp.EEG.File.FileType.BrainVision;
                files = new string[] { brainVisionDataContainer.Header };
            }
            else if (dataInfo.DataContainer is Container.EDF edfDataContainer)
            {
                type = Tools.CSharp.EEG.File.FileType.EDF;
                files = new string[] { edfDataContainer.File };
            }
            else if (dataInfo.DataContainer is Container.FIF fifDataContainer)
            {
                type = Tools.CSharp.EEG.File.FileType.FIF;
                files = new string[] { fifDataContainer.File };
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            Tools.CSharp.EEG.File file = new Tools.CSharp.EEG.File(type, true, files);
            if (file.getHandle().Handle == IntPtr.Zero)
            {
                throw new Exception("Data file could not be loaded");
            }
            List<Tools.CSharp.EEG.Electrode> channels = file.Electrodes;
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
            Frequency = new Tools.CSharp.EEG.Frequency(0);
        }
        #endregion
    }
}