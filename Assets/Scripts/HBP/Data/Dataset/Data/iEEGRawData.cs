using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public class IEEGRawData : DynamicData
    {
        #region Properties
        /// <summary>
        /// Patient of the data.
        /// </summary>
        public Patient Patient { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new Data instance with default values.
        /// </summary>
        public IEEGRawData(): this(new Dictionary < string, float[] >(), new Dictionary<string, string>(), new Tools.CSharp.EEG.Frequency(), new Patient())
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
        public IEEGRawData(Dictionary<string,float[]> valuesBySite, Dictionary<string,string> unitBySite, Tools.CSharp.EEG.Frequency frequency, Patient patient): base(valuesBySite, unitBySite, frequency)
        {
            Patient = patient;
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="dataInfo">DataInfo to read.</param>
        /// <param name="MNI">\a True if MNI and \a false otherwise.</param>
        public IEEGRawData(IEEGDataInfo dataInfo) : base(dataInfo)
        {
            Patient = dataInfo.Patient;
        }
        #endregion
    }
}