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
        public Protocol Protocol { get; set; }
        public Dictionary<int,Window> TimeLimitsByColumn { get; set; }
        #endregion

        #region Constructor
        public TrialMatrix(DataInfo dataInfo, string channel, IEnumerable<Experience.Protocol.Bloc> blocsToDisplay = null)
        {
            Protocol protocol = dataInfo.Dataset.Protocol;
            ChannelData channelData = DataManager.GetData(dataInfo, channel);

            // Genreate blocs.
            if (blocsToDisplay == null) blocsToDisplay = dataInfo.Dataset.Protocol.Blocs;
            IEnumerable<Bloc> blocs = protocol.Blocs.Where(bloc => blocsToDisplay.Contains(bloc)).Select(bloc => new Bloc(bloc, channelData.DataByBloc[bloc]));

            //Standardize Blocs
            Standardize(blocs);

            // Set properties
            Title = "Site: " + channel + "   |   Patient: " + dataInfo.Patient.CompleteName + "   |   Protocol: " + protocol.Name + "   |   Data: " + dataInfo.Name;
            Blocs = blocs.ToArray();
            Limits = blocs.SelectMany(b => b.SubBlocs).SelectMany(s => s.SubTrials).SelectMany(v => v.Data.Values).ToArray().CalculateValueLimit();
            Protocol = dataInfo.Dataset.Protocol;
            //TimeLimitsByColumn = CalculateTimeLimitsByColumn(blocs);
        }
        #endregion

        #region Private Method
        void Standardize(IEnumerable<Bloc> blocs)
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

            Dictionary<int, Tuple<int, int>> limitsByColumns = new Dictionary<int, Tuple<int, int>>(subBlocsByColumns.Count);
            for (int column = 0; column < subBlocsByColumns.Count; column++)
            {
                List<SubBloc> subBlocs = subBlocsByColumns[column];
                int before = subBlocs.Max(s => s.SubTrials.First().Data.InformationsByEvent[s.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                int after = subBlocs.Max(s => s.SubTrials.First().Data.Values.Length - s.SubTrials.First().Data.InformationsByEvent[s.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                limitsByColumns.Add(column, new Tuple<int, int>(before, after));
                foreach (SubBloc subBloc in subBlocs)
                {
                    subBloc.SpacesBefore = before - subBloc.SubTrials.First().Data.InformationsByEvent[subBloc.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart;
                    subBloc.SpacesAfter = after - (subBloc.SubTrials.First().Data.Values.Length - subBloc.SubTrials.First().Data.InformationsByEvent[subBloc.SubBlocProtocol.MainEvent].Occurences.First().IndexFromStart);
                }
            }
        }
        Dictionary<int,Window> CalculateTimeLimitsByColumn(IEnumerable<Bloc> blocs)
        {
            Dictionary<int, List<SubBloc>> subBlocsByColumns = new Dictionary<int, List<SubBloc>>();
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

            Dictionary<int, Window> timeLimitsByColumns = new Dictionary<int, Window>();
            foreach (var pair in subBlocsByColumns)
            {
                List<SubBloc> subBlocs = subBlocsByColumns[pair.Key];
                timeLimitsByColumns.Add(pair.Key, new Window(subBlocs.Min(s => s.SubBlocProtocol.Window.Start), subBlocs.Max(s => s.SubBlocProtocol.Window.End)));
            }
            return timeLimitsByColumns;
        }
        #endregion
    }
}