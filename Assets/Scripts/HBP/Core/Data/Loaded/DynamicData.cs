using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Exceptions;
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
        public DynamicData(DataInfo dataInfo) : this(EEGRecordingSource.From(dataInfo))
        {
        }
        internal DynamicData(EEGRecordingSource source) : this()
        {
            // Read Data.
            string[] missingFiles = source.ReaderFiles.Where(filePath => !string.IsNullOrWhiteSpace(filePath) && !System.IO.File.Exists(filePath)).ToArray();
            if (missingFiles.Length > 0)
            {
                throw new DataFileNotFoundException(missingFiles);
            }
            using DLL.EEG.File file = new(source.FileType, true, source.ReaderFiles);
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
        }
        #endregion
    }
}
