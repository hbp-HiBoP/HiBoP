using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Anatomy;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using Tools.CSharp;

namespace HBP.Data.TrialMatrix
{
    public class TrialMatrix
    {
        #region Properties
        string title;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        Bloc[] blocs;
        public Bloc[] Blocs
        {
            get { return blocs; }
            set { blocs = value; }
        }

        int nbColumns;
        public int NbColumns
        {
            get { return nbColumns; }
             set { nbColumns = value; }
        }

        Vector2 valuesLimits;
        public Vector2 ValuesLimits
        {
            get { return valuesLimits; }
             set { valuesLimits = value; }
        }

        Protocol protocol;
        public Protocol Protocol
        {
            get { return protocol; }
            set { protocol = value; }
        }

        Vector2[] timeLimitsByColumn;
        public Vector2[] TimeLimitsByColumn
        {
            get { return timeLimitsByColumn; }
            set { timeLimitsByColumn = value; }
        }
        #endregion

        #region Constructor
        public TrialMatrix(DataInfo dataInfo, PlotID plot)
        {
            // Read ElanFile and POSFile.
            Elan.ElanFile elanFile = new Elan.ElanFile(dataInfo.EEG);
            Localizer.POS POSFile = new Localizer.POS(dataInfo.POS);
            float samplingFrequency = elanFile.EEG.SamplingFrequency;

            // Read Data.
            Elan.Track trackToRead = elanFile.FindTrack(dataInfo.Measure, plot.Name);
            elanFile.ReadChannel(trackToRead);
            float[] dataReaded = elanFile.EEG.GetFloatData(trackToRead);

            // Epoch Data.
            Bloc[] blocs = new Bloc[dataInfo.Protocol.Blocs.Count];
            for (int i = 0; i < blocs.Length; i++)
            {
                blocs[i] = new Bloc(dataInfo.Protocol.Blocs[i], POSFile, dataReaded, samplingFrequency);
            }

            blocs = (from bloc in blocs where bloc.Lines.Length > 0 select bloc).ToArray();

            // Standardize bloc by BaseLine.
            StandardizeLinesByBaseLine(ref blocs);

            // Calculate values limits.
            List<float> flatValues = new List<float>();
            foreach (Bloc bloc in blocs)
            {
                foreach (Line line in bloc.Lines)
                {
                    flatValues.AddRange(line.DataWithCorrection);
                }
            }

            //Standardize Blocs
            NbColumns = CalculateNbColumn(blocs);
            StandardizeBlocs(ref blocs);

            // Set properties
            Title = plot.Patient.Place + " " + plot.Patient.Date + " " + plot.Patient.Name + " " + plot.Name + " " + dataInfo.Protocol.Name + " " + dataInfo.Name;
            Blocs = blocs;
            ValuesLimits = CalculateValueLimit(flatValues.ToArray());
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(blocs);
            Protocol = dataInfo.Protocol;
        }
        #endregion

        #region Private Method

        void StandardizeBlocs(ref Bloc[] blocs)
        {
            // Initiate index.
            int[] beforeByColumns = new int[NbColumns];
            int[] afterByColumns = new int[NbColumns];
            for (int i = 0; i < NbColumns; i++)
            {
                beforeByColumns[i] = int.MinValue;
                afterByColumns[i] = int.MinValue;
            }

            // Find min and max index.
            foreach(Bloc bloc in blocs)
            {
                int before = bloc.Lines[0].Main.Position;
                int after = bloc.Lines[0].Data.Length - before;
                int col = bloc.PBloc.DisplayInformations.Position.Column - 1;
                if (before > beforeByColumns[col])
                {
                    beforeByColumns[col] = before;
                }
                if(after > afterByColumns[col])
                {
                    afterByColumns[col] = after;
                }
            }

            // Standardize blocs
            foreach (Bloc bloc in blocs)
            {
                int col = bloc.PBloc.DisplayInformations.Position.Column - 1;
                int lenght = beforeByColumns[col] + afterByColumns[col];
                int min = beforeByColumns[col] - bloc.Lines[0].Main.Position;
                int max = bloc.Lines[0].Data.Length + min - 1;
                foreach (Line line in bloc.Lines)
                {    
                    float[] l_data = new float[lenght];
                    for (int i = 0; i < l_data.Length; i++)
                    {
                        if((i < min) || (i > max))
                        {
                            l_data[i] = float.NaN;
                        }
                        else
                        {
                            l_data[i] = line.Data[i - min];
                        }
                    }
                    line.Data = l_data;
                    line.Main.Position += min;
                    foreach(Event e in line.Secondaries)
                    {
                        e.Position += min;
                    }
                }
            }
        }

        void StandardizeLinesByBaseLine(ref Bloc[] blocs)
        {
            float average = 0;
            float standardDeviation = 1;
            switch (ApplicationState.GeneralSettings.TrialMatrixSettings.Baseline)
            {
                case Settings.TrialMatrixSettings.BaselineType.None:
                    foreach (Bloc b in blocs)
                    {
                        foreach (Line l in b.Lines)
                        {
                            average = 0;
                            standardDeviation = 1;
                            l.CorrectionByBaseLine(average, standardDeviation);
                        }
                    }
                    break;
                case Settings.TrialMatrixSettings.BaselineType.Line:
                    foreach (Bloc b in blocs)
                    {
                        foreach (Line l in b.Lines)
                        {
                            average = Tools.CSharp.MathfExtension.Average(l.BaseLine);
                            standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(l.BaseLine);
                            l.CorrectionByBaseLine(average, standardDeviation);
                        }
                    }
                    break;
                case Settings.TrialMatrixSettings.BaselineType.Bloc:
                    foreach (Bloc b in blocs)
                    {
                        List<float> l_blocBaseLine = new List<float>();
                        foreach (Line l in b.Lines)
                        {
                            l_blocBaseLine.AddRange(l.BaseLine);
                        }
                        average = Tools.CSharp.MathfExtension.Average(l_blocBaseLine.ToArray());
                        standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(l_blocBaseLine.ToArray());
                        foreach (Line l in b.Lines)
                        {
                            l.CorrectionByBaseLine(average, standardDeviation);
                        }
                    }
                    break;
                case Settings.TrialMatrixSettings.BaselineType.Protocol:
                    List<float> protocol = new List<float>();
                    foreach (Bloc b in blocs)
                    {
                        foreach (Line l in b.Lines)
                        {
                            protocol.AddRange(l.BaseLine);
                        }
                    }
                    average = Tools.CSharp.MathfExtension.Average(protocol.ToArray());
                    standardDeviation = Tools.CSharp.MathfExtension.StandardDeviation(protocol.ToArray());
                    foreach (Bloc b in blocs)
                    {
                        foreach (Line l in b.Lines)
                        {
                            l.CorrectionByBaseLine(average, standardDeviation);
                        }
                    }
                    break;
            }
        }
        Vector2[] CalculateTimeLimitsByColumn(Bloc[] blocs)
        {
            Vector2[] l_limits = new Vector2[NbColumns];
            for (int i = 0; i < NbColumns; i++)
            {
                l_limits[i] = new Vector2(float.MaxValue, float.MinValue);
            }
            foreach(Bloc bloc in blocs)
            {
                Window window = bloc.PBloc.DisplayInformations.Window;
                int col = bloc.PBloc.DisplayInformations.Position.Column - 1;
                if(l_limits[col].x > window.Start)
                {
                    l_limits[col].x = window.Start;
                }
                if (l_limits[col].y < window.End)
                {
                    l_limits[col].y = window.End;
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
                if (l_nbColumn < bloc.PBloc.DisplayInformations.Position.Column)
                {
                    l_nbColumn = bloc.PBloc.DisplayInformations.Position.Column;
                }
            }
            return l_nbColumn;
        }
        #endregion
    }
}