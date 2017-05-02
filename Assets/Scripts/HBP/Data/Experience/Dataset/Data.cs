using System.Collections.Generic;

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
    *   - \a Values readed.
    *   - \a Plot masks.
    *   - \a POS file.
    *   - \a EEG frequency.
    *   - \a Patient.
    */
    public class Data
    {
        #region Properties
        float[][] values;
        /// <summary>
        /// Plot values.
        /// </summary>
        public float[][] Values { get { return values; } set { values = value; } }

        bool[] mask = new bool[0];
        /// <summary>
        /// Plot mask : \a True if masked and \a false otherwise.
        /// </summary>
        public bool[] Mask { get { return mask; } set { mask = value; } }

        Localizer.POS pos;
        /// <summary>
        /// POS file which containts plot anatomy informations.
        /// </summary>
        public Localizer.POS POS { get { return pos; } set { pos = value; } }

        float frequency;
        /// <summary>
        /// Values frequency.
        /// </summary>
        public float Frequency { get { return frequency; } set { frequency = value; } }

        Patient patient;
        /// <summary>
        /// Patient 
        /// </summary>
        public Patient Patient { get { return patient; } set { patient = value; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Create a new Data instance with default values.
        /// </summary>
        public Data()
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
        public Data(float[][] values,bool[] mask,Localizer.POS pos,float frequency,Patient patient)
        {
            Values = values;
            Mask = mask;
            POS = pos;
            Frequency = frequency;
            Patient = patient;
        }
        /// <summary>
        /// Create a new Data instance.
        /// </summary>
        /// <param name="info">DataInfo to read.</param>
        /// <param name="MNI">\a True if MNI and \a false otherwise.</param>
        public Data(DataInfo info, bool MNI)
        {
            // Read Elan.
            Elan.ElanFile elanFile = new Elan.ElanFile(info.EEG,true);
            string[] measures = elanFile.MeasureLabels;
            Elan.Channel[] channels = elanFile.Channels;

            // Read implantation.
            if(MNI)
            {
                info.Patient.Brain.LoadImplantation(Anatomy.Implantation.ReferenceFrameType.MNI,true);
            }
            else
            {
                info.Patient.Brain.LoadImplantation(Anatomy.Implantation.ReferenceFrameType.Patient, true);
            }

            string[] plots = info.Patient.Brain.GetImplantation(MNI,ApplicationState.GeneralSettings.PlotNameAutomaticCorrectionType == Settings.GeneralSettings.PlotNameCorrectionTypeEnum.Active).GetPlotsName();
            bool[] maskPlots = new bool[plots.Length];
            float[][] values = new float[plots.Length][];

            // Find channel to read.
            List<Elan.Track> tracksToRead = new List<Elan.Track>();
            for (int p = 0; p < plots.Length; p++)
            {
                Elan.Track track = elanFile.FindTrack(info.Measure, plots[p]);
                if (track.Measure < 0 || track.Channel < 0)
                {
                    maskPlots[p] = true;
                }
                else
                {
                    maskPlots[p] = false;
                    tracksToRead.Add(track);
                }
            }

            // Read data.
            float[][] dataReaded = elanFile.EEG.GetFloatData(tracksToRead.ToArray());
            int sampleNumber = elanFile.EEG.SampleNumber;

            int n = 0;
            for (int p = 0; p < plots.Length; p++)
            {
                if (maskPlots[p])
                {
                    values[p] = new float[sampleNumber];
                }
                else
                {
                    values[p] = dataReaded[n];
                    n++;
                }
            }

            // Set properties.
            Values = values;
            Mask = maskPlots;
            POS = new Localizer.POS(info.POS);
            Frequency = elanFile.EEG.SamplingFrequency;
            Patient = info.Patient;
        }
        #endregion
    }
}