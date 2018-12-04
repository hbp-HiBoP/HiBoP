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
        public List<Tuple<int,Window>> TimeLimitsByColumn { get; set; }
        #endregion

        #region Constructor
        public TrialMatrix(DataInfo dataInfo, string channel, IEnumerable<Experience.Protocol.Bloc> blocsToDisplay = null)
        {
            Protocol protocol = dataInfo.Dataset.Protocol;
            ChannelData channelData = DataManager.GetData(dataInfo, channel);

            // Genreate blocs.
            if (blocsToDisplay == null) blocsToDisplay = dataInfo.Dataset.Protocol.Blocs;
            Bloc[] blocs = protocol.Blocs.Where(bloc => blocsToDisplay.Contains(bloc)).Select(bloc => new Bloc(bloc, channelData.DataByBloc[bloc])).ToArray();

            //Standardize Blocs
            Standardize(blocs);

            // Set properties
            Title = channel + " (" + dataInfo.Patient.Name + ") " + dataInfo.Dataset.Name + " " + dataInfo.Name;
            Blocs = blocs;
            Limits = blocs.SelectMany(b => b.SubBlocs).SelectMany(s => s.SubTrials).SelectMany(v => v.Data.Values).ToArray().CalculateValueLimit();
            DataInfo = dataInfo;
            Channel = channel;
            TimeLimitsByColumn = CalculateTimeLimitsByColumn(blocs);
        }
        #endregion

        #region Private Method
        void Standardize(Bloc[] blocs)
        {
            Dictionary<int,List<SubBloc>> subBlocsByColumns = new Dictionary<int, List<SubBloc>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.ProtocolBloc.MainSubBlocPosition;             
                for (int i = 0; i < bloc.SubBlocs.Length; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.ContainsKey(column)) subBlocsByColumns[column] = new List<SubBloc>();
                    subBlocsByColumns[i - mainSubBlocPosition].Add(bloc.SubBlocs[i]);
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
        List<Tuple<int,Window>> CalculateTimeLimitsByColumn(IEnumerable<Bloc> blocs)
        {
            List<Tuple<int, List<SubBloc>>> subBlocsByColumns = new List<Tuple<int, List<SubBloc>>>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.ProtocolBloc.MainSubBlocPosition;
                for (int i = 0; i < bloc.SubBlocs.Length; i++)
                {
                    int column = i - mainSubBlocPosition;
                    if (!subBlocsByColumns.Any(t => t.Item1 == column)) subBlocsByColumns.Add(new Tuple<int,List<SubBloc>>(column,new List<SubBloc>()));
                    subBlocsByColumns.Find(t => t.Item1 == i - mainSubBlocPosition).Item2.Add(bloc.SubBlocs[i]);
                }
            }

            List<Tuple<int, Window>> timeLimitsByColumns = new List<Tuple<int, Window>>();
            foreach (var tuple in subBlocsByColumns)
            {
                List<SubBloc> subBlocs = tuple.Item2;
                timeLimitsByColumns.Add( new Tuple<int,Window>(tuple.Item1, new Window(subBlocs.Min(s => s.SubBlocProtocol.Window.Start), subBlocs.Max(s => s.SubBlocProtocol.Window.End))));
            }
            return timeLimitsByColumns.OrderBy(tuple => tuple.Item1).ToList();
        }
        #endregion
    }
}