using System.Collections.Generic;

namespace HBP.Core.Data
{
    public class CCEPRawData : DynamicData
    {
        #region Properties
        /// <summary>
        /// Channel stimulated.
        /// </summary>
        public string StimulatedChannel { get; set; }
        /// <summary>
        /// Patient.
        /// </summary>
        public Patient Patient { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new CCEP Data instance.
        /// </summary>
        public CCEPRawData() : this("", new Dictionary<string, float[]>(), new Dictionary<string, string>(), new Tools.Frequency(), new Patient())
        {
        }
        /// <summary>
        /// Create a new CCEP Data instance.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="unit"></param>
        /// <param name="frequency"></param>
        /// <param name="patient"></param>
        public CCEPRawData(string channelStimulated, Dictionary<string, float[]> valuesBySite, Dictionary<string, string> unitBySite, Tools.Frequency frequency, Patient patient) : base(valuesBySite, unitBySite, frequency)
        {
            StimulatedChannel = channelStimulated;
            Patient = patient;
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="dataInfo">DataInfo</param>
        public CCEPRawData(CCEPDataInfo dataInfo) : base(dataInfo)
        {
            Patient = dataInfo.Patient;
            StimulatedChannel = dataInfo.StimulatedChannel;
        }
        #endregion
    }
}