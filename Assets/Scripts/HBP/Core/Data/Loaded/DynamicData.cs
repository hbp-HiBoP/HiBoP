using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.Core.Data
{
    public class DynamicData
    {
        #region Properties
        public virtual Dictionary<string, float[]> ValuesByChannel { get; set; }
        public virtual Dictionary<string, string> UnitByChannel { get; set; }
        public virtual Tools.Frequency Frequency { get; set; }
        protected Dictionary<int, List<EventOccurence>> m_OccurencesByCode;
        #endregion

        #region Public Methods
        public virtual IEnumerable<EventOccurence> GetOccurences(IEnumerable<int> codes)
        {
            return from code in codes from occurence in GetOccurences(code) select occurence;
        }
        public virtual IEnumerable<EventOccurence> GetOccurences(int code)
        {
            return m_OccurencesByCode.ContainsKey(code) ? from occurence in m_OccurencesByCode[code] select occurence : new List<EventOccurence>();
        }
        #endregion

        #region Constructors
        public DynamicData() : this(new Dictionary<string, float[]>(), new Dictionary<string, string>(), new Tools.Frequency())
        {
        }
        public DynamicData(Dictionary<string, float[]> valuesBySite, Dictionary<string, string> unitBySite, Tools.Frequency frequency)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            Frequency = frequency;
        }
        public DynamicData(DataInfo dataInfo) : this()
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
            else if (dataInfo.DataContainer is Container.Elan elanDataContainer)
            {
                type = DLL.EEG.File.FileType.ELAN;
                files = new string[] { elanDataContainer.EEG, elanDataContainer.POS, elanDataContainer.Notes };
            }
            else if (dataInfo.DataContainer is Container.Micromed micromedDataContainer)
            {
                type = DLL.EEG.File.FileType.Micromed;
                files = new string[] { micromedDataContainer.Path };
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
                try
                {
                    ValuesByChannel.Add(channel.Label, channel.Data);
                    UnitByChannel.Add(channel.Label, channel.Unit);
                }
                catch (ArgumentException e)
                {
                    Debug.LogException(e);
                    throw new Exception(string.Format("The data file contains multiple {0} channels.", channel.Label));
                }
            }
            Frequency = file.SamplingFrequency;
            m_OccurencesByCode = new Dictionary<int, List<EventOccurence>>();
            List<DLL.EEG.Trigger> events = file.Triggers;
            foreach (var _event in events)
            {
                int code = _event.Code;
                int sample = (int)_event.Sample;
                if (!m_OccurencesByCode.ContainsKey(code)) m_OccurencesByCode[code] = new List<EventOccurence>();
                m_OccurencesByCode[code].Add(new EventOccurence(code, sample, Frequency.ConvertNumberOfSamplesToRoundedMilliseconds(sample)));
            }
            file.Dispose();
        }
        #endregion
    }
}