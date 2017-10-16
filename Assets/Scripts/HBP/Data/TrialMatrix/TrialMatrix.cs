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
        public string Title { get; set; }
        public Bloc[] Blocs { get; set; }
        public Vector2 ValuesLimits { get; set; }
        public Protocol Protocol { get; set; }
        public Vector2[] TimeLimitsByColumn { get; set; }
        #endregion

        #region Constructor
        public TrialMatrix(Protocol protocol, DataInfo dataInfo, Dictionary<Experience.Protocol.Bloc,Localizer.Bloc[]> blocsByProtocolBloc, Module3D.Site site)
        {
            // Genreate blocs.
            UnityEngine.Profiling.Profiler.BeginSample("Generate blocs");
            Bloc[] trialMatrixBlocs = (from bloc in protocol.Blocs select new Bloc(bloc, blocsByProtocolBloc[bloc] , site)).ToArray();
            UnityEngine.Profiling.Profiler.EndSample();

            Normalize(trialMatrixBlocs, site);

            // Calculate values limits.
            UnityEngine.Profiling.Profiler.BeginSample("calculate values");
            List<float> values = new List<float>();
            foreach (Bloc bloc in trialMatrixBlocs) foreach (Line line in bloc.Lines) values.AddRange(line.Bloc.NormalizedValuesBySite[site.Information.FullCorrectedID]);
            ValuesLimits = CalculateValueLimit(values.ToArray());
            UnityEngine.Profiling.Profiler.EndSample();

            //Standardize Blocs
            UnityEngine.Profiling.Profiler.BeginSample("standardize blocs");
            Standardize(trialMatrixBlocs, site);
            UnityEngine.Profiling.Profiler.EndSample();

            // Set properties
            UnityEngine.Profiling.Profiler.BeginSample("set properties");
            Title = dataInfo.Patient.Place + " " + dataInfo.Patient.Date + " " + dataInfo.Patient.Name + " " + site.Information.Name + " " + protocol.Name + " " + dataInfo.Name;
            Blocs = trialMatrixBlocs.ToArray();
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(trialMatrixBlocs);
            Protocol = protocol;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Private Method
        void Standardize(Bloc[] blocs, Module3D.Site site)
        {
            // Initiate index.
            int columnNumber = (from bloc in blocs select bloc.ProtocolBloc.DisplayInformations.Position.Column).Max();
            int[] beforeByColumns = new int[columnNumber];
            int[] afterByColumns = new int[columnNumber];
            for (int c = 0; c < columnNumber; c++)
            {
                beforeByColumns[c] = ((from bloc in blocs where (bloc.ProtocolBloc.DisplayInformations.Position.Column - 1 == c) select bloc.Lines.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]).Max());
                afterByColumns[c] = ((from bloc in blocs where (bloc.ProtocolBloc.DisplayInformations.Position.Column - 1 == c) select bloc.Lines.First().Bloc.ValuesBySite.First().Value.Length - bloc.Lines.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]).Max());
            }

            // Standardize blocs
            foreach (Bloc bloc in blocs)
            {
                int col = bloc.ProtocolBloc.DisplayInformations.Position.Column - 1;
                bloc.SpacesBefore = beforeByColumns[col] - bloc.Lines.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent];
                bloc.SpacesAfter = afterByColumns[col] - (bloc.Lines.First().Bloc.ValuesBySite.First().Value.Length - bloc.Lines.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]);
            }
        }
        void Normalize(Bloc[] blocs, Module3D.Site site)
        {
            foreach (Bloc bloc in blocs)
            {
                foreach (Line line in bloc.Lines)
                {
                    line.UpdateValues();
                }
            }
        }
        Vector2[] CalculateTimeLimitsByColumn(Bloc[] blocs)
        {
            int columnNumber = (from bloc in blocs select bloc.ProtocolBloc.DisplayInformations.Position.Column).Max();
            Vector2[] limits = new Vector2[columnNumber];
            for (int i = 0; i < columnNumber; i++)
            {
                IEnumerable<Bloc> blocsInColumn = blocs.Where((b) => b.ProtocolBloc.DisplayInformations.Position.Column - 1 == i);
                limits[i] = new Vector2(blocsInColumn.Min((b) => b.ProtocolBloc.DisplayInformations.Window.Start), blocsInColumn.Max((b) => b.ProtocolBloc.DisplayInformations.Window.End));
            }
            return limits;
        }
        Vector2 CalculateValueLimit(IEnumerable<float> values)
        {
            float mean = values.Average();
            float sum = 0;
            foreach (float value in values)
            {
                float delta = value - mean;
                sum += Mathf.Pow(delta, 2);
            }
            float standardDeviation = Mathf.Sqrt(sum / (values.Count() - 1));
            return new Vector2(mean - 1.96f * Mathf.Abs(standardDeviation), mean + 1.96f * Mathf.Abs(standardDeviation));
        }
        #endregion
    }
}