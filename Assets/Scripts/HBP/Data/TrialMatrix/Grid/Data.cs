using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;

namespace HBP.Data.TrialMatrix.Grid
{
    public class Data
    {
        #region Properties
        public string Title { get; set; }
        public Bloc[] Blocs { get; set; }
        public Vector2 Limits { get; set; }
        public bool UseAutoLimits { get; set; }
        public List<Tuple<int, Window>> TimeLimitsByColumn { get; set; }
        public DataStruct DataStruct { get; set; }
        public ChannelStruct[] ChannelStructs { get; set; }
        #endregion

        #region Constructors
        public Data(DataStruct dataStruct, ChannelStruct[] channelStructs, IEnumerable<Experience.Protocol.Bloc> blocsToDisplay = null)
        {
            Title = dataStruct.Dataset.Name + " " + dataStruct.Data;

            if (blocsToDisplay == null) blocsToDisplay = dataStruct.Dataset.Protocol.Blocs;
            Blocs = blocsToDisplay.Select(bloc => new Bloc(bloc, dataStruct, channelStructs)).ToArray();

            Limits = CalculateLimits(Blocs);
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(blocsToDisplay);

            DataStruct = dataStruct;
            ChannelStructs = channelStructs;
        }
        #endregion

        #region Private Methods
        Vector2 CalculateLimits(IEnumerable<Bloc> blocs)
        {
            List<float> values = new List<float>();
            foreach (var bloc in blocs)
            {
                foreach(var channelBloc in bloc.ChannelBlocs)
                {
                    foreach (var subBloc in channelBloc.SubBlocs)
                    {
                        foreach (var subTrial in subBloc.SubTrials)
                        {
                            values.AddRange(subTrial.Data.Values);
                        }
                    }
                }
            }
            return values.ToArray().CalculateValueLimit();
        }
        List<Tuple<int, Window>> CalculateTimeLimitsByColumn(IEnumerable<Experience.Protocol.Bloc> blocs)
        {
            List<Tuple<int, List<Experience.Protocol.SubBloc>>> subBlocsByColumns = new List<Tuple<int, List<Experience.Protocol.SubBloc>>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                for (int i = 0; i < bloc.SubBlocs.Count; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.Any(t => t.Item1 == column)) subBlocsByColumns.Add(new Tuple<int, List<Experience.Protocol.SubBloc>>(column, new List<Experience.Protocol.SubBloc>()));
                    subBlocsByColumns.Find(t => t.Item1 == i - mainSubBlocPosition).Item2.Add(bloc.SubBlocs[i]);
                }
            }

            List<Tuple<int, Window>> timeLimitsByColumns = new List<Tuple<int, Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                List<Experience.Protocol.SubBloc> subBlocs = tuple.Item2;
                timeLimitsByColumns.Add(new Tuple<int, Window>(tuple.Item1, new Window(subBlocs.Min(s => s.Window.Start), subBlocs.Max(s => s.Window.End))));
            }
            return timeLimitsByColumns.OrderBy(tuple => tuple.Item1).ToList();
        }
        void Standardize(Bloc[] blocs)
        {
            Dictionary<int, List<Experience.Protocol.SubBloc>> subBlocsByColumns = new Dictionary<int, List<Experience.Protocol.SubBloc>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.Data.MainSubBlocPosition;
                for (int i = 0; i < bloc.Data.SubBlocs.Count; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.ContainsKey(column)) subBlocsByColumns[column] = new List<Experience.Protocol.SubBloc>();
                    subBlocsByColumns[i - mainSubBlocPosition].Add(bloc.Data.SubBlocs[i]);
                }
            }

            foreach (var pair in subBlocsByColumns)
            {
                int before = pair.Value.Max(s => s.SubTrials.First(sub => sub.Data.Found).Data.InformationsByEvent[s.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                int after = pair.Value.Max(s => s.SubTrials.First(sub => sub.Data.Found).Data.Values.Length - s.SubTrials.First(sub => sub.Data.Found).Data.InformationsByEvent[s.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                foreach (SubBloc subBloc in pair.Value)
                {
                    subBloc.SpacesBefore = before - subBloc.SubTrials.First(sub => sub.Data.Found).Data.InformationsByEvent[subBloc.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart;
                    subBloc.SpacesAfter = after - (subBloc.SubTrials.First(sub => sub.Data.Found).Data.Values.Length - subBloc.SubTrials.First(sub => sub.Data.Found).Data.InformationsByEvent[subBloc.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                }
            }
        }
        #endregion
    }
}