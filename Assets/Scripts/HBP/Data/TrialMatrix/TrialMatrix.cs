using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Experience.Dataset;
using HBP.Data.Patient;

namespace HBP.Data.TrialMatrix
{
    public class TrialMatrix
    {
        #region Properties
        string m_title;
        public string Title { get { return m_title; } set { m_title = value; } }

        Bloc[] m_blocs;
        public Bloc[] Blocs { get { return m_blocs; } set { m_blocs = value; } }

        int m_nbColumn;
        public int NbColumn { get { return m_nbColumn; } set { m_nbColumn = value; } }

        Vector2 m_valuesLimits;
        public Vector2 ValuesLimits { get { return m_valuesLimits; } set { m_valuesLimits = value; } }

        Experience.Protocol.Protocol m_protocol;
        public Experience.Protocol.Protocol Protocol { get { return m_protocol; } }

        Vector2[] m_timeLimitsByColumn;
        public Vector2[] TimeLimitsByColumn { get { return m_timeLimitsByColumn; } set { m_timeLimitsByColumn = value; } }
        #endregion

        #region Constructor
        public TrialMatrix(DataInfo dataInfo, PlotID plot)
        {
            m_protocol = dataInfo.Protocol;

            Title = plot.Patient.Place +" "+ plot.Patient.Date+" "+ plot.Patient.Name+" "+plot.Name+" "+ dataInfo.Protocol.Name+ " " + dataInfo.Name;

            //Read Header and Pos
            Localizer.EEG l_EEG = new Localizer.EEG(dataInfo.EEG);
            Localizer.POS l_POS = new Localizer.POS(dataInfo.POS);

            //Read Data
            float[] l_data = ReadData(l_EEG, dataInfo.Measure, plot.Name);

            //Epoch Data
            m_blocs = new Bloc[dataInfo.Protocol.Blocs.Count];
            int l_iMax = dataInfo.Protocol.Blocs.Count;
            for (int i = 0; i < l_iMax; i++)
            {
                m_blocs[i] = new Bloc(dataInfo.Protocol.Blocs[i], l_POS, l_data, l_EEG.Header.SamplingFrequency);
            }

            // Standardize by BaseLine
            StandardizeLinesByBaseLine(ref m_blocs);

            //Calculate NbColumn
            NbColumn = CalculateNbColumn(m_blocs);

            //CalculateLimits
            List<float> l_values = new List<float>();
            foreach (Bloc bloc in m_blocs)
            {
                foreach (Line line in bloc.Lines)
                {
                    l_values.AddRange(line.DataWithCorrection);
                }
            }
            ValuesLimits = CalculateValueLimit(l_values.ToArray());
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(m_blocs);

            //Standardize Blocs
            StandardizeBlocs(ref m_blocs);
        }
        #endregion

        #region Private Methods
        float[] ReadData(Localizer.EEG eeg, string measureLabel, string plot)
        {
            Localizer.Measure l_measure = new Localizer.Measure();
            Localizer.Channel l_channel = new Localizer.Channel();
            l_measure.Label = string.Empty;
            l_channel.Label = string.Empty;
            l_channel.Type = string.Empty;
            l_channel.Unite = string.Empty;
            foreach (Localizer.Measure measure in eeg.Header.Measures)
            {
                if (measure.Label == measureLabel)
                {
                    l_measure = measure;
                    break;
                }
            }
            foreach (Localizer.Channel channel in eeg.Header.Channels)
            {
                if (channel.Label == plot)
                {
                    l_channel = channel;
                    break;
                }
            }
            Localizer.Header.DataChannel l_dataChannel = new Localizer.Header.DataChannel(l_measure, l_channel);
            return eeg.ReadData(l_dataChannel);
        }

        void StandardizeBlocs(ref Bloc[] blocs)
        {
            // Initiate index
            int[] l_minIndex = new int[NbColumn];
            int[] l_maxIndex = new int[NbColumn];
            for (int i = 0; i < NbColumn; i++)
            {
                l_minIndex[i] = int.MinValue;
                l_maxIndex[i] = int.MinValue;
            }

            // Find min and max index
            foreach(Bloc bloc in blocs)
            {
                int l_min = bloc.Lines[0].Main.Position;
                int l_max = bloc.Lines[0].Data.Length - bloc.Lines[0].Main.Position;
                int col = bloc.PBloc.DisplayInformations.Column - 1;
                if (l_min > l_minIndex[col])
                {
                    l_minIndex[col] = l_min;
                }
                if(l_max > l_maxIndex[col])
                {
                    l_maxIndex[col] = l_max;
                }
            }

            // Standardize blocs
            foreach (Bloc bloc in blocs)
            {
                int col = bloc.PBloc.DisplayInformations.Column-1;
                int lenght = l_minIndex[col] + l_maxIndex[col];
                int minIndex = l_minIndex[col] - bloc.Lines[0].Main.Position;
                int maxIndex = bloc.Lines[0].Data.Length + minIndex - 1;
                foreach (Line line in bloc.Lines)
                {    
                    float[] l_data = new float[lenght];
                    for (int i = 0; i < l_data.Length; i++)
                    {
                        if((i < minIndex) || (i > maxIndex))
                        {
                            l_data[i] = float.NaN;
                        }
                        else
                        {
                            l_data[i] = line.Data[i - minIndex];
                        }
                    }
                    line.Data = l_data;
                    line.Main.Position += minIndex;
                    foreach(Event e in line.Secondaries)
                    {
                        e.Position += minIndex;
                    }
                }
            }
        }

        void StandardizeLinesByBaseLine(ref Bloc[] blocs)
        {
            if (ApplicationState.GeneralSettings.BaseLineType == Settings.GeneralSettings.BaseLineTypeEnum.None)
            {
                foreach (Bloc b in blocs)
                {
                    foreach (Line l in b.Lines)
                    {
                        float average = 0;
                        float standardDeviation = 1;
                        l.CorrectionByBaseLine(average, standardDeviation);
                    }
                }
            }
            else if (ApplicationState.GeneralSettings.BaseLineType == Settings.GeneralSettings.BaseLineTypeEnum.Line)
            {
                foreach(Bloc b in blocs)
                {
                    foreach(Line l in b.Lines)
                    {
                        float average = Tools.CSharp.MathfExtension.Average(l.BaseLine);
                        float standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(l.BaseLine);
                        l.CorrectionByBaseLine(average, standardDeviation);
                    }
                }
            }
            else if (ApplicationState.GeneralSettings.BaseLineType == Settings.GeneralSettings.BaseLineTypeEnum.Bloc)
            {
                foreach(Bloc b in blocs)
                {
                    List<float> l_blocBaseLine = new List<float>();
                    foreach(Line l in b.Lines)
                    {
                        l_blocBaseLine.AddRange(l.BaseLine);
                    }
                    float average = Tools.CSharp.MathfExtension.Average(l_blocBaseLine.ToArray());
                    float standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(l_blocBaseLine.ToArray());
                    foreach(Line l in b.Lines)
                    {
                        l.CorrectionByBaseLine(average, standardDeviation);
                    }
                }
            }
            else if (ApplicationState.GeneralSettings.BaseLineType == Settings.GeneralSettings.BaseLineTypeEnum.Protocol)
            {
                List<float> l_protocolBaseLine = new List<float>();
                foreach (Bloc b in blocs)
                {
                    foreach (Line l in b.Lines)
                    {
                        l_protocolBaseLine.AddRange(l.BaseLine);
                    }
                }
                float average = Tools.CSharp.MathfExtension.Average(l_protocolBaseLine.ToArray());
                float standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(l_protocolBaseLine.ToArray());
                foreach (Bloc b in blocs)
                {
                    foreach (Line l in b.Lines)
                    {
                        l.CorrectionByBaseLine(average, standardDeviation);
                    }
                }
            }
        }


        Vector2[] CalculateTimeLimitsByColumn(Bloc[] blocs)
        {
            Vector2[] l_limits = new Vector2[NbColumn];
            for (int i = 0; i < NbColumn; i++)
            {
                l_limits[i] = new Vector2(float.MaxValue, float.MinValue);
            }
            foreach(Bloc bloc in blocs)
            {
                Vector2 window = bloc.PBloc.DisplayInformations.Window;
                int col = bloc.PBloc.DisplayInformations.Column-1;
                if(l_limits[col].x > window.x)
                {
                    l_limits[col].x = window.x;
                }
                if (l_limits[col].y < window.y)
                {
                    l_limits[col].y = window.y;
                }
            }
            return l_limits;
        }

        Vector2 CalculateValueLimit(float[] values)
        {
            float moyenne = values.Average();
            float somme = 0;
            int length = values.Length;
            for (int i = 0; i < length; i++)
            {
                float delta = values[i] - moyenne;
                somme += delta * delta;
            }
            float l_standarDeviation = Mathf.Sqrt(somme / (values.Length - 1));
            return new Vector2(moyenne - 1.96f * Mathf.Abs(l_standarDeviation), moyenne + 1.96f * Mathf.Abs(l_standarDeviation));
        }

        int CalculateNbColumn(Bloc[] blocs)
        {
            // Find nbColumn
            int l_nbColumn = 0;
            foreach (Bloc bloc in blocs)
            {
                if (l_nbColumn < bloc.PBloc.DisplayInformations.Column)
                {
                    l_nbColumn = bloc.PBloc.DisplayInformations.Column;
                }
            }
            return l_nbColumn;
        }
        #endregion
    }
}