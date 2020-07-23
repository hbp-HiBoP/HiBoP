using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
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
        public Vector2 Limits { get; set; }
        public DataInfo DataInfo { get; set; }
        public string Channel { get; set; }
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
                    IEnumerable<SubBloc> subBlocs = Blocs.SelectMany(c => c.SubBlocs).Where(s => pair.Item1.Contains(s.SubBlocProtocol));
                    foreach (var subBloc in subBlocs)
                    {
                        subBloc.Window = pair.Item2;
                    }
                }
            }
        }
        #endregion

        #region Constructor
        public TrialMatrix(IEEGDataInfo dataInfo, string channel, IEnumerable<Experience.Protocol.Bloc> blocsToDisplay = null)
        {
            Protocol protocol = dataInfo.Dataset.Protocol;
            ChannelData channelData = DataManager.GetData(dataInfo, channel);

            // Genreate blocs.
            if (blocsToDisplay == null) blocsToDisplay = dataInfo.Dataset.Protocol.Blocs;
            Bloc[] blocs = protocol.Blocs.Where(bloc => blocsToDisplay.Contains(bloc)).Select(bloc => new Bloc(bloc, channelData.DataByBloc[bloc])).ToArray();

            // Set properties
            Title = channel + " (" + dataInfo.Patient.Name + ") " + dataInfo.Dataset.Name + " " + dataInfo.Name;
            Blocs = blocs;
            Limits = blocs.SelectMany(b => b.SubBlocs).SelectMany(s => s.SubTrials).SelectMany(v => v.Data.Values).ToArray().CalculateValueLimit();
            DataInfo = dataInfo;
            Channel = channel;
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(blocsToDisplay);
        }
        #endregion

        #region Private Method
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
                timeLimitsByColumns.Add(new Tuple<Experience.Protocol.SubBloc[], Window>(tuple.Item2.ToArray(), window));
            }
            return timeLimitsByColumns;
        }
        #endregion
    }
}