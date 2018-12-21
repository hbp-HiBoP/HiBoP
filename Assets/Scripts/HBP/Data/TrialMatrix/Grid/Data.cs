using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using p = HBP.Data.Experience.Protocol;

namespace HBP.Data.TrialMatrix.Grid
{
    public class Data
    {
        #region Properties
        public string Title { get; set; }
        public Bloc[] Blocs { get; set; }
        public Vector2 Limits { get; set; }
        public Tuple<p.SubBloc[], Window>[] SubBlocsAndWindowByColumn { get; }
        public DataStruct DataStruct { get; set; }
        public ChannelStruct[] ChannelStructs { get; set; }
        #endregion

        #region Constructors
        public Data(DataStruct dataStruct, ChannelStruct[] channelStructs)
        {
            Title = dataStruct.Dataset.Name + " " + dataStruct.Data;
            Blocs = dataStruct.Blocs.Select(bloc => new Bloc(bloc, dataStruct, channelStructs)).ToArray();

            Limits = CalculateLimits(Blocs);
            SubBlocsAndWindowByColumn = GetSubBlocsAndWindowByColumn(dataStruct.Blocs);
            foreach (var bloc in Blocs)
            {
                foreach (var channelBloc in bloc.ChannelBlocs)
                {
                    channelBloc.Standardize(SubBlocsAndWindowByColumn);
                }
            }

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
                    if(channelBloc.IsFound)
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
            }
            return values.ToArray().CalculateValueLimit();
        }
        Tuple<p.SubBloc[], Window>[] GetSubBlocsAndWindowByColumn(IEnumerable<p.Bloc> blocs)
        {
            List<Tuple<int, List<p.SubBloc>>> subBlocsByColumns = new List<Tuple<int, List<p.SubBloc>>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                p.SubBloc[] orderedSubBlocs = bloc.OrderedSubBlocs.ToArray();
                for (int i = 0; i < orderedSubBlocs.Length; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.Any(t => t.Item1 == column)) subBlocsByColumns.Add(new Tuple<int, List<p.SubBloc>>(column, new List<p.SubBloc>()));
                    subBlocsByColumns.Find(t => t.Item1 == column).Item2.Add(orderedSubBlocs[i]);
                }
            }
            subBlocsByColumns = subBlocsByColumns.OrderBy(t => t.Item1).ToList();

            List<Tuple<p.SubBloc[], Window>> timeLimitsByColumns = new List<Tuple<p.SubBloc[], Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                Window window = new Window(tuple.Item2.Min(s => s.Window.Start), tuple.Item2.Max(s => s.Window.End));
                //foreach (var subBloc in tuple.Item2)
                //{
                //    subBloc.Window = window;
                //}
                timeLimitsByColumns.Add( new Tuple<p.SubBloc[], Window>(tuple.Item2.ToArray(), window));
            }
            return timeLimitsByColumns.ToArray();
        }
        #endregion
    }
}