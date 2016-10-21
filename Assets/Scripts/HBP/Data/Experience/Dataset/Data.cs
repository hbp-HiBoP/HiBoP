using System.IO;
using System.Collections.Generic;

namespace HBP.Data.Experience.Dataset
{
    public class Data
    {
        #region Properties
        float[][] m_values;
        public float[][] Values { get { return m_values; } set { m_values = value; } }

        bool[] m_mask = new bool[0];
        public bool[] Mask { get { return m_mask; } set { m_mask = value; } }

        Localizer.POS m_pos;
        public Localizer.POS POS { get { return m_pos; } set { m_pos = value; } }

        float m_frequency;
        public float Frequency { get { return m_frequency; } set { m_frequency = value; } }

        Patient.Patient m_patient;
        public Patient.Patient Patient { get { return m_patient; } set { m_patient = value; } }
        #endregion

        #region Constructor
        public Data()
        {
        }

        public Data(float[][] values,bool[] mask,Localizer.POS pos,float frequency,Patient.Patient patient)
        {
            Values = values;
            Mask = mask;
            POS = pos;
            Frequency = frequency;
            Patient = patient;
        }

        public Data(DataInfo info, bool MNI)
        {
            Patient = info.Patient;

            //Read POS File.
            POS = new Localizer.POS(info.POS);

            // Read EEG Header.
            Localizer.EEG l_EEG = new Localizer.EEG(info.EEG);
            Frequency = l_EEG.Header.SamplingFrequency;

            List<bool> l_mask = new List<bool>();
            List<float[]> l_values = new List<float[]>();

            // Find channel to read.
            Patient.Electrode[] l_electrodes = info.Patient.Brain.ReadImplantation(MNI);
            List<Patient.Plot> l_plots = new List<Patient.Plot>();
            List<Localizer.Header.DataChannel> l_dataChannelToRead = new List<Localizer.Header.DataChannel>();
            foreach (Localizer.Measure measure in l_EEG.Header.Measures)
            {
                if (measure.Label == info.Measure)
                {
                    foreach (Patient.Electrode electrode in l_electrodes)
                    {
                        foreach (Patient.Plot plot in electrode.Plots)
                        {
                            l_plots.Add(plot);
                            bool l_found = false;
                            foreach (Localizer.Channel channel in l_EEG.Header.Channels)
                            {
                                if (channel.Label == plot.Name)
                                {
                                    l_mask.Add(false);
                                    l_dataChannelToRead.Add(new Localizer.Header.DataChannel(measure, channel));
                                    l_found = true;
                                    break;
                                }
                            }
                            if (!l_found)
                            {
                                l_mask.Add(true);
                            }
                        }
                    }
                }
            }

            // Read data.

            float[][] l_data = l_EEG.ReadData(l_dataChannelToRead.ToArray());
            int p = 0;
            if(l_data.Length == l_dataChannelToRead.Count)
            {
                int size = l_EEG.Header.SampleNumber;
                for (int i = 0; i < l_mask.Count; i++)
                {
                    if (l_mask[i])
                    {
                        l_values.Add(new float[size]);
                    }
                    else
                    {
                        l_values.Add(l_data[p]);
                        p++;
                    }
                }
            }
            else
            {
                throw new FileLoadException();
            }
            Values = l_values.ToArray();
            Mask = l_mask.ToArray();
        }
        #endregion
    }
}