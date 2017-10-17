using System.Collections.Generic;
using HBP.Data.Anatomy;

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
    public class Data
    {
        #region Properties
        /// <summary>
        /// Site values.
        /// </summary>
        public Dictionary<string,float[]> ValuesBySite { get; set; }
        /// <summary>
        /// Plot mask : \a True if masked and \a false otherwise.
        /// </summary>
        public Dictionary<string,bool> MaskBySite { get; set; }
        /// <summary>
        /// POS file which containts plot anatomy informations.
        /// </summary>
        public Localizer.POS POS { get; set; }
        /// <summary>
        /// Frequency of data.
        /// </summary>
        public float Frequency { get; set; }
        /// <summary>
        /// Patient of the data.
        /// </summary>
        public Patient Patient { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Data instance with default values.
        /// </summary>
        public Data(): this(new Dictionary < string, float[] >(), new Dictionary < string, bool >(), new Localizer.POS(), 0, new Patient())
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
        public Data(Dictionary<string,float[]> valuesBySite, Dictionary<string, bool> maskBySite, Localizer.POS pos, float frequency, Patient patient)
        {
            ValuesBySite = valuesBySite;
            MaskBySite = maskBySite;
            POS = pos;
            Frequency = frequency;
            Patient = patient;
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="info">DataInfo to read.</param>
        /// <param name="MNI">\a True if MNI and \a false otherwise.</param>
        public Data(DataInfo info) : this()
        {
            // Read Elan.
            Elan.ElanFile elanFile = new Elan.ElanFile(info.EEG,true);
            Elan.Channel[] channels = elanFile.Channels;
            foreach (var channel in channels)
            {
                Elan.Track track = elanFile.FindTrack(info.Measure, channel.Label);
                if (track.Channel >= 0 && track.Measure >= 0)
                {
                    ValuesBySite.Add(info.Patient.ID + "_" + channel.Label,elanFile.EEG.GetFloatData(track)); // fixme
                    MaskBySite.Add(channel.Label, false);
                }
            }
            POS = new Localizer.POS(info.POS);
            Frequency = elanFile.EEG.SamplingFrequency;
            Patient = info.Patient;
        }
        #endregion
    }
}