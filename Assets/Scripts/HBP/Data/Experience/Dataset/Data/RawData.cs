using System.Collections.Generic;
using HBP.Data.Localizer;

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
        /// POS file which containts plot anatomy informations.
        /// </summary>
        public POS POS { get; set; }
        /// <summary>
        /// Frequency of data.
        /// </summary>
        public Frequency Frequency { get; set; }
        /// <summary>
        /// Patient of the data.
        /// </summary>
        public Patient Patient { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Data instance with default values.
        /// </summary>
        public RawData(): this(new Dictionary < string, float[] >(), new Dictionary<string, string>(), new POS(),  new Frequency(), new Patient())
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
        public RawData(Dictionary<string,float[]> valuesBySite, Dictionary<string,string> unitBySite, POS pos, Frequency frequency, Patient patient)
        {
            ValuesByChannel = valuesBySite;
            UnitByChannel = unitBySite;
            POS = pos;
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
            // Read Elan.
            Elan.ElanFile elanFile = new Elan.ElanFile(info.EEG,true);
            Elan.Channel[] channels = elanFile.Channels;
            foreach (var channel in channels)
            {
                Elan.Track track = elanFile.FindTrack(info.Measure, channel.Label);
                if (track.Channel >= 0 && track.Measure >= 0)
                {
                    ValuesByChannel.Add(channel.Label, elanFile.EEG.GetFloatData(track));
                    UnitByChannel.Add(channel.Label, channel.Unit);
                }
            }
            POS = new POS(info.POS, new Frequency(elanFile.EEG.SamplingFrequency));
            Frequency = new Frequency(elanFile.EEG.SamplingFrequency);
            Patient = info.Patient;
            elanFile.Dispose();
        }
        #endregion
    }
}