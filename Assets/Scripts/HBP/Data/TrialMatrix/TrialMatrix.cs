using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Module3D;

namespace HBP.Data.TrialMatrix
{
    public class TrialMatrix
    {
        #region Properties
        public string Title { get; set; }
        public Bloc[] Blocs { get; set; }
        public Vector2 Limits { get; set; }
        public Protocol Protocol { get; set; }
        public Vector2[] TimeLimitsByColumn { get; set; }
        #endregion

        #region Constructor
        public TrialMatrix(Protocol protocol, DataInfo dataInfo, Dictionary<Experience.Protocol.Bloc,Localizer.Bloc[]> blocsByProtocolBloc, Module3D.Site site, Base3DScene scene)
        {
            // Genreate blocs.
            UnityEngine.Profiling.Profiler.BeginSample("Generate blocs");
            Bloc[] trialMatrixBlocs;
            if (ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol)
            {
                trialMatrixBlocs = (from bloc in protocol.Blocs select new Bloc(bloc, blocsByProtocolBloc[bloc], site)).ToArray();
            }
            else
            {
                List<Bloc> blocs = new List<Bloc>();
                foreach (var bloc in protocol.Blocs)
                {
                    if (scene.Visualization.Columns.Exists((c) => c.Bloc == bloc))
                    {
                        blocs.Add(new Bloc(bloc, blocsByProtocolBloc[bloc], site));
                    }
                }
                trialMatrixBlocs = blocs.ToArray();
            }
            UnityEngine.Profiling.Profiler.EndSample();

            Normalize(trialMatrixBlocs, site);

            // Calculate values limits.
            UnityEngine.Profiling.Profiler.BeginSample("calculate values");
            List<float> values = new List<float>();
            foreach (Bloc bloc in trialMatrixBlocs) foreach (Line line in bloc.Trials) values.AddRange(line.Bloc.NormalizedValuesBySite[site.Information.FullCorrectedID]);
            Limits = CalculateValueLimit(values.ToArray());
            UnityEngine.Profiling.Profiler.EndSample();

            //Standardize Blocs
            UnityEngine.Profiling.Profiler.BeginSample("standardize blocs");
            Standardize(trialMatrixBlocs, site);
            UnityEngine.Profiling.Profiler.EndSample();

            // Set properties
            UnityEngine.Profiling.Profiler.BeginSample("set properties");
            Title = "Site: " + site.Information.ChannelName + "   |   Patient: " + dataInfo.Patient.CompleteName + "   |   Protocol: " + protocol.Name + "   |   Data: " + dataInfo.Name;
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
            int columnNumber = (from bloc in blocs select bloc.ProtocolBloc.Position.Column).Max();
            int[] beforeByColumns = new int[columnNumber];
            int[] afterByColumns = new int[columnNumber];
            for (int c = 0; c < columnNumber; c++)
            {
                beforeByColumns[c] = ((from bloc in blocs where (bloc.ProtocolBloc.Position.Column - 1 == c) select bloc.Trials.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]).Max());
                afterByColumns[c] = ((from bloc in blocs where (bloc.ProtocolBloc.Position.Column - 1 == c) select bloc.Trials.First().Bloc.ValuesBySite.First().Value.Length - bloc.Trials.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]).Max());
            }

            // Standardize blocs
            foreach (Bloc bloc in blocs)
            {
                int col = bloc.ProtocolBloc.Position.Column - 1;
                bloc.SpacesBefore = beforeByColumns[col] - bloc.Trials.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent];
                bloc.SpacesAfter = afterByColumns[col] - (bloc.Trials.First().Bloc.ValuesBySite.First().Value.Length - bloc.Trials.First().Bloc.PositionByEvent[bloc.ProtocolBloc.MainEvent]);
            }
        }
        void Normalize(Bloc[] blocs, Module3D.Site site)
        {
            foreach (Bloc bloc in blocs)
            {
                foreach (Line line in bloc.Trials)
                {
                    line.UpdateValues();
                }
            }
        }
        Vector2[] CalculateTimeLimitsByColumn(Bloc[] blocs)
        {
            int columnNumber = (from bloc in blocs select bloc.ProtocolBloc.Position.Column).Max();
            Vector2[] limits = new Vector2[columnNumber];
            for (int i = 0; i < columnNumber; i++)
            {
                IEnumerable<Bloc> blocsInColumn = blocs.Where((b) => b.ProtocolBloc.Position.Column - 1 == i);
                limits[i] = new Vector2(blocsInColumn.Min((b) => b.ProtocolBloc.Window.Start), blocsInColumn.Max((b) => b.ProtocolBloc.Window.End));
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