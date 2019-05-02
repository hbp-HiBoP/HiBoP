using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Experience.Dataset
{
    /**
    * \class Data
    * \author Adrien Gannerie
    * \version 1.0
    * \date 09 janvier 2017
    * \brief Data readed from a DataInfo.
    * 
    * \detail Data readed from a DataInfo which contains :
    *   - \a Values readed by site.
    *   - \a IsMasked by site.
    *   - \a POS file.
    *   - \a EEG frequency.
    *   - \a Patient.
    */
    public class RawData
    {
        #region Properties
        /// <summary>
        /// Site values.
        /// </summary>
        public Dictionary<string,float[]> ValuesByChannel { get; set; }
        /// <summary>
        /// Site values.
        /// </summary>
        public Dictionary<string, string> UnitByChannel { get; set; }
        /// <summary>
        /// Occurences of events by code
        /// </summary>
        Dictionary<int, List<Occurence>> m_OccurencesByCode;
        /// <summary>
        /// Frequency of data.
        /// </summary>
        public Tools.CSharp.EEG.Frequency Frequency { get; set; }
        /// <summary>
        /// Patient of the data.
        /// </summary>
        public Patient Patient { get; set; }
        #endregion

        #region Public Methods
        public IEnumerable<Occurence> GetOccurences(IEnumerable<int> codes)
        {
            return from code in codes from occurence in GetOccurences(code) select occurence;
        }
        public IEnumerable<Occurence> GetOccurences(int code)
        {
            return m_OccurencesByCode.ContainsKey(code) ? from occurence in m_OccurencesByCode[code] select occurence : new List<Occurence>();
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Data instance with default values.
        /// </summary>
        public RawData(): this(new Dictionary < string, float[] >(), new Dictionary<string, string>(), new Tools.CSharp.EEG.Frequency(), new Patient())
        {
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="values">Plots values.</param>
        /// <param name="mask">Plots mask.</param>
        /// <param name="pos">POS file.</param>
        /// <param name="frequency">Values frequency.</param>
        /// <param name="patient">Patient.</param>
        public RawData(Dictionary<string,float[]> valuesBySite, Dictionary<string,string> unitBySite, Tools.CSharp.EEG.Frequency frequency, Patient patient)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            Frequency = frequency;
            Patient = patient;
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="info">DataInfo to read.</param>
        /// <param name="MNI">\a True if MNI and \a false otherwise.</param>
        public RawData(DataInfo info) : this()
        {
            // Read Data.
            Tools.CSharp.EEG.File file = null;
            if (info is ElanDataInfo elanDataInfo)
            {
                file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.ELAN, true, elanDataInfo.EEG, elanDataInfo.POS, elanDataInfo.Notes);
            }
            else if (info is EdfDataInfo edfDataInfo)
            {
                file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.EDF, true, edfDataInfo.EDF);
            }
            else if (info is BrainVisionDataInfo brainVisionDataInfo)
            {
                file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.BrainVision, true, brainVisionDataInfo.Header);
            }
            else if (info is MicromedDataInfo micromedDataInfo)
            {
                file = new Tools.CSharp.EEG.File(Tools.CSharp.EEG.File.FileType.Micromed, true, micromedDataInfo.TRC);
            }
            else
            {
                throw new System.Exception("Invalid file format");
            }
            List<Tools.CSharp.EEG.Electrode> channels = file.Electrodes;
            foreach (var channel in channels)
            {
                ValuesByChannel.Add(channel.Label, channel.Data);
                UnitByChannel.Add(channel.Label, channel.Unit);
            }
            Frequency = file.SamplingFrequency;
            m_OccurencesByCode = new Dictionary<int, List<Occurence>>();
            List<Tools.CSharp.EEG.Trigger> events = file.Triggers;
            foreach (var _event in events)
            {
                int code = _event.Code;
                int sample = (int)_event.Sample;
                if (!m_OccurencesByCode.ContainsKey(code)) m_OccurencesByCode[code] = new List<Occurence>();
                m_OccurencesByCode[code].Add(new Occurence(code, sample, Frequency.ConvertNumberOfSamplesToRoundedMilliseconds(sample)));
            }
            Patient = info.Patient;
            file.Dispose();
        }
        #endregion
        
        #region Struct
        public struct Occurence
        {
            #region Properties
            public int Code { get; set; }
            public int Index { get; set; }
            public float Time { get; set; }
            #endregion

            #region Constructors
            public Occurence(int code, int index, float time = -1)
            {
                Code = code;
                Index = index;
                Time = time;
            }
            #endregion
        }
        #endregion
    }
}