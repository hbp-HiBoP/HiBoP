using HBP.Data.Patient;
using System.Collections.Generic;

namespace HBP.Data.Visualisation
{
    public class ColumnData
    {
        #region Properties
        private string m_label;
        /// <summary>
        /// Label of the column.
        /// </summary>
        public string Label { get { return m_label; } set { m_label = value; } }

        private bool[] m_maskPlot;
        /// <summary>
        /// Mask which define if a plot have to be display.
        /// if(true) display.
        /// else don't display.
        /// </summary>
        public bool[] MaskPlot { get { return m_maskPlot; } set { m_maskPlot = value; } }

        private TimeLine m_timeLine;
        /// <summary>
        /// TimeLine which define the size,limits,events.
        /// </summary>
        public TimeLine TimeLine { get { return m_timeLine; } set { m_timeLine = value; } }

        private IconicScenario m_iconicScenario;
        /// <summary>
        /// Iconic scenario which define the labels,images to display during the timeLine. 
        /// </summary>
        public IconicScenario IconicScenario { get { return m_iconicScenario; } set { m_iconicScenario = value; } }

        private float[][] m_value;
        /// <summary>
        /// Values of the data for each plots.
        /// float[x][ ] : Plots.
        /// float[ ][x] : values of the plot selected.
        /// </summary>
        public float[][] Values { get { return m_value; } set { m_value = value; } }
        #endregion

        #region Constructor

        public ColumnData() : this("",new bool[0],new TimeLine(),new IconicScenario(),new float[0][])
        {
        }

        public ColumnData(string label,bool[] maskPlot,TimeLine timeLine,IconicScenario iconicScenario,float[][] values)
        {
            Label = label;
            MaskPlot = maskPlot;
            TimeLine = timeLine;
            IconicScenario = iconicScenario;
            Values = values;
        }

        public ColumnData(Patient.Patient[] patients, Experience.Dataset.Data[] data, Column column)
        {
            Label = "Data : " + column.DataLabel + " Dataset : " + column.Dataset.Name + " Protocol : " + column.Protocol.Name + " Bloc : " + column.Bloc.DisplayInformations.Name;
            List<bool> l_mask = new List<bool>();
            List<float[]> l_value = new List<float[]>();
            Localizer.Bloc l_bloc = new Localizer.Bloc();
            Experience.Dataset.Data l_data = new Experience.Dataset.Data();
            foreach(Patient.Patient patient in patients)
            {
                foreach(Experience.Dataset.Data t_data in data)
                {
                    if(t_data.Patient == patient)
                    {
                        l_mask.AddRange(t_data.Mask);
                        Localizer.Bloc[] l_blocs = Localizer.Bloc.AverageBlocs(t_data.Values, t_data.POS, column.Bloc, t_data.Frequency);
                        l_bloc = l_blocs[0];
                        l_data = t_data;
                        foreach (Localizer.Bloc bloc in l_blocs)
                        {
                            l_value.Add(bloc.Data);
                        }
                    }
                }
            }

            // Set TimeLine
            Event l_mainEvent = new Event(column.Bloc.MainEvent.Name, l_bloc.MainEventPosition);
            Event[] l_secondaryEvent = new Event[column.Bloc.SecondaryEvents.Count];
            for (int p = 0; p < l_secondaryEvent.Length; p++)
            {
                l_secondaryEvent[p] = new Event(column.Bloc.SecondaryEvents[p].Name, l_bloc.SecondaryEventsPosition[p]);
            }
            TimeLine = new TimeLine(column.Bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, l_data.Frequency);

            // Set Scenario
            IconicScenario = new IconicScenario(column.Bloc, l_data.Frequency, TimeLine);

            Values = l_value.ToArray();
            MaskPlot = l_mask.ToArray();
        }

        public ColumnData(Experience.Dataset.Data data, Column column)
        {
            Label = "Data : " + column.DataLabel + " Dataset : " + column.Dataset.Name + " Protocol : " + column.Protocol.Name + " Bloc : " + column.Bloc.DisplayInformations.Name;
            MaskPlot = data.Mask;
            Localizer.Bloc[] l_blocs = Localizer.Bloc.AverageBlocs(data.Values, data.POS, column.Bloc, data.Frequency);

            // Set TimeLine
            Event l_mainEvent = new Event(column.Bloc.MainEvent.Name, l_blocs[0].MainEventPosition);
            Event[] l_secondaryEvent = new Event[column.Bloc.SecondaryEvents.Count];
            for (int p = 0; p < l_secondaryEvent.Length; p++)
            {
                l_secondaryEvent[p] = new Event(column.Bloc.SecondaryEvents[p].Name, l_blocs[0].SecondaryEventsPosition[p]);
            }
            TimeLine = new TimeLine(column.Bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, data.Frequency);

            // Set Scenario
            IconicScenario = new IconicScenario(column.Bloc, data.Frequency, TimeLine);

            // Set the data
            List<float[]> l_values = new List<float[]>();
            for (int p = 0; p < MaskPlot.Length; p++)
            {
                    l_values.Add(l_blocs[p].Data);
            }
            Values = l_values.ToArray();
        }

        public ColumnData(SinglePatientVisualisation visualisation, int column)
        {
            // Init Lists.
            List<bool> l_maskPlot = new List<bool>();
            List<float[]> l_values = new List<float[]>();

            // read ExperiencesData and Electrodes.
            Column col = visualisation.Columns[column];
            Experience.Dataset.DataInfo[] l_dataInfo = visualisation.GetDataInfo(col);
            Experience.Protocol.Bloc l_bloc = col.Bloc;
            Label = col.DataLabel + " of " + col.Dataset.Name + "display with "+col.Bloc.DisplayInformations.Name + " of " + col.Protocol.Name; 
            string l_pts = visualisation.GetImplantation();
            Electrode[] l_electrodes = Electrode.readImplantationFile(l_pts);

            // Set List and patient
            Patient.Patient l_patient = visualisation.Patient;
            List<Localizer.Header.DataChannel> l_dataChannelToRead = new List<Localizer.Header.DataChannel>();
            List<Plot> l_plots = new List<Plot>();
            List<bool> l_plotsMask = new List<bool>();

            // Find the expData of the patient.
            foreach (Experience.Dataset.DataInfo t_dataInfo in l_dataInfo)
            {
                if (t_dataInfo.Patient == l_patient)
                {
                    Localizer.EEG l_EEG = new Localizer.EEG(t_dataInfo.EEG);
                    Localizer.POS l_POS = new Localizer.POS(t_dataInfo.POS);

                    // Detect the dataChannelToRead
                    foreach (Localizer.Measure measure in l_EEG.Header.Measures)
                    {
                        if (measure.Label == t_dataInfo.Measure)
                        {
                            foreach (Electrode electrode in l_electrodes)
                            {
                                foreach (Plot plot in electrode.Plots)
                                {
                                    l_plots.Add(plot);
                                    bool l_found = false;
                                    foreach (Localizer.Channel channel in l_EEG.Header.Channels)
                                    {
                                        if (channel.Label == plot.Name)
                                        {
                                            l_plotsMask.Add(false);
                                            l_dataChannelToRead.Add(new Localizer.Header.DataChannel(measure, channel));
                                            l_found = true;
                                            break;
                                        }
                                    }
                                    if (!l_found)
                                    {
                                        l_plotsMask.Add(true);
                                    }
                                }
                            }
                        }

                        // Read all the channels,epoch and average
                        System.Diagnostics.Stopwatch l_stopWatch = new System.Diagnostics.Stopwatch();
                        l_stopWatch.Start();
                        float[][] l_data = l_EEG.ReadData(l_dataChannelToRead.ToArray());
                        l_stopWatch.Stop();
                        UnityEngine.Debug.Log("Read : "+l_stopWatch.ElapsedMilliseconds);
                        l_stopWatch.Reset();
                        l_stopWatch.Start();
                        Localizer.Bloc[] l_blocs = Localizer.Bloc.AverageBlocs(l_data, l_POS, l_bloc, l_EEG.Header.SamplingFrequency);
                        l_stopWatch.Stop();
                        UnityEngine.Debug.Log("Epoch : "+l_stopWatch.ElapsedMilliseconds);
                        if (l_dataChannelToRead.Count != 0)
                        {
                            // Set TimeLine
                            Event l_mainEvent = new Event(l_bloc.MainEvent.Name, l_blocs[0].MainEventPosition);
                            Event[] l_secondaryEvent = new Event[l_bloc.SecondaryEvents.Count];
                            for (int p = 0; p < l_secondaryEvent.Length; p++)
                            {
                                l_secondaryEvent[p] = new Event(l_bloc.SecondaryEvents[p].Name, l_blocs[0].SecondaryEventsPosition[p]);
                            }
                            TimeLine = new TimeLine(l_bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, l_EEG.Header.SamplingFrequency);

                            // Set Scenario
                            IconicScenario = new IconicScenario(l_bloc, l_EEG.Header.SamplingFrequency,TimeLine);
                        }
                        else
                        {
                            Event l_mainEvent = new Event(l_bloc.MainEvent.Name, 0);
                            Event[] l_secondaryEvent = new Event[l_bloc.SecondaryEvents.Count];
                            for (int p = 0; p < l_secondaryEvent.Length; p++)
                            {
                                l_secondaryEvent[p] = new Event(l_bloc.SecondaryEvents[p].Name, 0);
                            }
                            TimeLine = new TimeLine(l_bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, l_EEG.Header.SamplingFrequency);

                            // Set Scenario
                            IconicScenario = new IconicScenario(l_bloc, l_EEG.Header.SamplingFrequency, TimeLine);
                        }

                        // Set the data
                        int l_nbPlots = l_plots.Count;
                        l_maskPlot.AddRange(l_plotsMask);
                        int n = 0;
                        for (int p = 0; p < l_nbPlots; p++)
                        {
                            if (!l_plotsMask[p])
                            {
                                l_values.Add(l_blocs[n].Data);
                                n++;
                            }
                            else
                            {
                                l_values.Add(new float[TimeLine.Size]);
                            }
                        }
                    }
                }
            }

            // Set MaskPlot
            MaskPlot = l_maskPlot.ToArray();

            // Set Values
            Values = l_values.ToArray();
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="visualisation">Visualisation set with the UI.</param>
        /// <param name="column">Nb of column to load.</param>
        public ColumnData(MultiPatientsVisualisation visualisation, int column)
        {
            // Init Lists.
            List<bool> l_maskPlot = new List<bool>();
            List<float[]> l_values = new List<float[]>();

            // read ExperiencesData and Electrodes.
            Column col = visualisation.Columns[column];
            Label = col.DataLabel + " of " + col.Dataset.Name + "display with " + col.Bloc.DisplayInformations.Name + " of " + col.Protocol.Name;
            Experience.Dataset.DataInfo[] l_dataInfo = visualisation.GetDataInfo(col);
            Experience.Protocol.Bloc l_bloc = col.Bloc;
            string[] l_pts = visualisation.GetImplantation();
            Electrode[][] l_electrodes = new Electrode[l_pts.Length][];
            for (int i = 0; i < l_electrodes.Length; i++)
            {
                l_electrodes[i] = Electrode.readImplantationFile(l_pts[i]);
            }

            // Read data for each Patients
            int l_nbPatient = visualisation.Patients.Count;
            for (int i = 0; i < l_nbPatient; i++)
            {
                // Set List and patient
                Patient.Patient l_patient = visualisation.Patients[i];
                List<Localizer.Header.DataChannel> l_dataChannelToRead = new List<Localizer.Header.DataChannel>();
                List<Plot> l_plots = new List<Plot>();
                List<bool> l_plotsMask = new List<bool>();

                // Find the expData of the patient.
                foreach (Experience.Dataset.DataInfo t_dataInfo in l_dataInfo)
                {
                    if (t_dataInfo.Patient == l_patient)
                    {
                        Localizer.EEG l_EEG = new Localizer.EEG(t_dataInfo.EEG);
                        Localizer.POS l_POS = new Localizer.POS(t_dataInfo.POS);

                        // Detect the dataChannelToRead
                        foreach (Localizer.Measure measure in l_EEG.Header.Measures)
                        {
                            if (measure.Label == t_dataInfo.Measure)
                            {
                                foreach (Electrode electrode in l_electrodes[i])
                                {
                                    foreach (Plot plot in electrode.Plots)
                                    {
                                        l_plots.Add(plot);
                                        bool l_found = false;
                                        foreach (Localizer.Channel channel in l_EEG.Header.Channels)
                                        {
                                            if (channel.Label == plot.Name)
                                            {
                                                l_plotsMask.Add(false);
                                                l_dataChannelToRead.Add(new Localizer.Header.DataChannel(measure, channel));
                                                l_found = true;
                                                break;
                                            }
                                        }
                                        if (!l_found)
                                        {
                                            l_plotsMask.Add(true);
                                        }
                                    }
                                }
                            }

                            // Read all the channels,epoch and average
                            Localizer.Bloc[] l_blocs = Localizer.Bloc.AverageBlocs(l_EEG.ReadData(l_dataChannelToRead.ToArray()), l_POS, l_bloc, l_EEG.Header.SamplingFrequency);
                            if (l_dataChannelToRead.Count != 0)
                            {
                                // Set TimeLine
                                Event l_mainEvent = new Event(l_bloc.MainEvent.Name, l_blocs[0].MainEventPosition);
                                Event[] l_secondaryEvent = new Event[l_bloc.SecondaryEvents.Count];
                                for (int p = 0; p < l_secondaryEvent.Length; p++)
                                {
                                    l_secondaryEvent[p] = new Event(l_bloc.SecondaryEvents[p].Name, l_blocs[0].SecondaryEventsPosition[p]);
                                }
                                TimeLine = new TimeLine(l_bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, l_EEG.Header.SamplingFrequency);

                                // Set Scenario
                                IconicScenario = new IconicScenario(l_bloc, l_EEG.Header.SamplingFrequency,TimeLine);
                            }
                            else
                            {
                                Event l_mainEvent = new Event(l_bloc.MainEvent.Name, 0);
                                Event[] l_secondaryEvent = new Event[l_bloc.SecondaryEvents.Count];
                                for (int p = 0; p < l_secondaryEvent.Length; p++)
                                {
                                    l_secondaryEvent[p] = new Event(l_bloc.SecondaryEvents[p].Name, 0);
                                }
                                TimeLine = new TimeLine(l_bloc.DisplayInformations, l_mainEvent, l_secondaryEvent, l_EEG.Header.SamplingFrequency);

                                // Set Scenario
                                IconicScenario = new IconicScenario(l_bloc, l_EEG.Header.SamplingFrequency,TimeLine);
                            }

                            // Set the data
                            int l_nbPlots = l_plots.Count;
                            l_maskPlot.AddRange(l_plotsMask);
                            int n = 0;
                            for (int p = 0; p < l_nbPlots; p++)
                            {
                                if (!l_plotsMask[p])
                                {
                                    l_values.Add(l_blocs[n].Data);
                                    n++;
                                }
                                else
                                {
                                    l_values.Add(new float[TimeLine.Size]);
                                }
                            }
                        }
                    }
                }
            }

            // Set MaskPlot
            MaskPlot = l_maskPlot.ToArray();

            // Set Values
            Values = l_values.ToArray();
        }
        #endregion

        #region  Public Methods
        /// <summary>
        /// Standardize data column.
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public void Standardize(int before, int after)
        {
            // Calculate differences.
            int l_difBefore = before - TimeLine.MainEvent.Position;
            int l_difAfter = after - (TimeLine.Size - TimeLine.MainEvent.Position);

            // Resize TimeLine.
            TimeLine.Resize(l_difBefore, l_difAfter);

            // Resize Iconic Scenario.
            IconicScenario.Reposition(l_difBefore);

            // Change Values
            for (int p = 0; p < Values.Length; p++)
            {
                float[] l_data = new float[Values[p].Length + l_difBefore + l_difAfter];
                for (int c = 0; c < l_data.Length; c++)
                {
                    if (c < l_difBefore)
                    {
                        l_data[c] = Values[p][0];
                    }
                    else if (c >= l_difBefore && c < Values[p].Length + l_difBefore)
                    {
                        l_data[c] = Values[p][c - l_difBefore];
                    }
                    else
                    {
                        l_data[c] = Values[p][Values[p].Length - 1];
                    }
                }
                Values[p] = l_data;
            }
        }
        #endregion
    }
}