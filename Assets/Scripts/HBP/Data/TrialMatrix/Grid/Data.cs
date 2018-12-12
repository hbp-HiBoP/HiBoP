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
        List<Tuple<Experience.Protocol.SubBloc[], Window>> m_TimeLimitsByColumn;
        public List<Tuple<Experience.Protocol.SubBloc[], Window>> TimeLimitsByColumn
        {
            get
            {
                return m_TimeLimitsByColumn;
            }
            private set
            {
                m_TimeLimitsByColumn = value;
                foreach (var pair in value)
                {
                    IEnumerable<SubBloc> subBlocs = Blocs.SelectMany(b => b.ChannelBlocs.SelectMany(c => c.SubBlocs).Where(s => pair.Item1.Contains(s.SubBlocProtocol)));
                    foreach (var subBloc in subBlocs)
                    {
                        subBloc.Window = pair.Item2;
                    }
                }
            }
        }
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
                    if(channelBloc.Found)
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
        List<Tuple<Experience.Protocol.SubBloc[], Window>> CalculateTimeLimitsByColumn(IEnumerable<Experience.Protocol.Bloc> blocs)
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

            List<Tuple<Experience.Protocol.SubBloc[], Window>> timeLimitsByColumns = new List<Tuple<Experience.Protocol.SubBloc[], Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                Window window = new Window(tuple.Item2.Min(s => s.Window.Start), tuple.Item2.Max(s => s.Window.End));
                foreach (var subBloc in tuple.Item2)
                {
                    subBloc.Window = window;
                }
                timeLimitsByColumns.Add( new Tuple<Experience.Protocol.SubBloc[], Window>(tuple.Item2.ToArray(), window));
            }
            return timeLimitsByColumns;
        }
        #endregion
    }
}