using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using p = HBP.Data.Experience.Protocol;

namespace HBP.Data.TrialMatrix.Grid
{
    public class ChannelBloc
    {
        #region Properties
        public bool IsFound { get; set; }
        public ChannelStruct Channel { get; set; }
        public p.Bloc Bloc { get; set; }
        public SubBloc[] SubBlocs { get; set; }

        bool m_IsHovered;
        public bool IsHovered
        {
            get
            {
                return m_IsHovered;
            }
            set
            {
                m_IsHovered = value;
                OnChangeIsHovered.Invoke(value);
            }
        }
        public BoolEvent OnChangeIsHovered;
        #endregion

        #region Constructors
        public ChannelBloc(p.Bloc bloc, DataStruct data, ChannelStruct channel)
        {
            DataInfo dataInfo = data.Dataset.Data.FirstOrDefault(d => d.Name == data.Data && d.Patient == channel.Patient);

            UnityEngine.Profiling.Profiler.BeginSample("GetData");
            BlocChannelData blocChannelData = DataManager.GetData(dataInfo, bloc, channel.Channel);
            UnityEngine.Profiling.Profiler.EndSample();

            IsFound = blocChannelData != null;
            Bloc = bloc;
            Channel = channel;
            if (IsFound)
            {
                List<SubBloc> subBlocs = new List<SubBloc>(bloc.SubBlocs.Count);
                IOrderedEnumerable<ChannelTrial> orderedTrials = SortTrials(bloc, blocChannelData.Trials.Where(t => t.IsValid)); // FIXME : Ajouter la gestion des trials non complets.
                foreach (var subBloc in bloc.OrderedSubBlocs)
                {
                    IEnumerable<SubTrial> subTrials = orderedTrials.Select(trial => new SubTrial(trial.ChannelSubTrialBySubBloc[subBloc]));
                    SubBloc dataSubBloc = new SubBloc(subBloc, subTrials.ToArray());
                    subBlocs.Add(dataSubBloc);
                }
                SubBlocs = subBlocs.ToArray();
            }
            else
            {
                List<SubBloc> subBlocs = new List<SubBloc>(bloc.SubBlocs.Count);
                foreach (var subBloc in bloc.OrderedSubBlocs)
                {
                    SubBloc dataSubBloc = new SubBloc(subBloc, new SubTrial[0]);
                    subBlocs.Add(dataSubBloc);
                }
                SubBlocs = subBlocs.ToArray();
            }
        }
        public void Standardize(Tuple<p.SubBloc[],Tools.CSharp.Window>[] subBlocsAndWindowByColumn)
        {
            List<SubBloc> subBlocs = SubBlocs.ToList();
            for (int c = 0; c < subBlocsAndWindowByColumn.Length; c++)
            {
                Tuple<p.SubBloc[], Tools.CSharp.Window> pair = subBlocsAndWindowByColumn[c];
                SubBloc subBloc = subBlocs.FirstOrDefault(s => pair.Item1.Contains(s.SubBlocProtocol));
                if (subBloc == null)
                {
                    subBlocs.Insert(c, new SubBloc(pair.Item2));
                }
                else
                {
                    subBloc.Window = pair.Item2;
                }
            }
            SubBlocs = subBlocs.ToArray();
        }
        #endregion

        #region Private Methods
        static IOrderedEnumerable<ChannelTrial> SortTrials(p.Bloc bloc, IEnumerable<ChannelTrial> trials)
        {
            IOrderedEnumerable<ChannelTrial> ordereredTrials = trials.OrderBy(t => t.IsValid);
            string[] orders = bloc.Sort.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = orders.Length - 1; i >= 0; i--)
            {
                string order = orders[i];
                string[] parts = order.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length == 3)
                {
                    string subBlocName = parts[0];
                    string eventName = parts[1];
                    string command = parts[2];
                    p.SubBloc subBloc = bloc.SubBlocs.FirstOrDefault(s => s.Name == subBlocName);
                    p.Event @event = subBloc.Events.FirstOrDefault(e => e.Name == eventName);
                    if(command == "LATENCY")
                    {
                        List<ChannelTrial> channelTrialsFound = new List<ChannelTrial>();
                        List<ChannelTrial> channelTrialsNotFound = new List<ChannelTrial>();
                        foreach (var channelTrial in ordereredTrials)
                        {
                            if(channelTrial.ChannelSubTrialBySubBloc[subBloc].InformationsByEvent[@event].IsFound)
                            {
                                channelTrialsFound.Add(channelTrial);
                            }
                            else
                            {
                                channelTrialsNotFound.Add(channelTrial);
                            }
                        }
                        ordereredTrials = channelTrialsFound.OrderBy(t => t.ChannelSubTrialBySubBloc[subBloc].InformationsByEvent[@event].Occurences.First().TimeFromMainEvent);
                        foreach (var channelTrial in channelTrialsNotFound)
                        {
                            ordereredTrials.Append(channelTrial);
                        }
                    }
                    else if(command == "CODE")
                    {
                        List<ChannelTrial> channelTrialsFound = new List<ChannelTrial>();
                        List<ChannelTrial> channelTrialsNotFound = new List<ChannelTrial>();
                        foreach (var channelTrial in ordereredTrials)
                        {
                            if (channelTrial.ChannelSubTrialBySubBloc[subBloc].InformationsByEvent[@event].IsFound)
                            {
                                channelTrialsFound.Add(channelTrial);
                            }
                            else
                            {
                                channelTrialsNotFound.Add(channelTrial);
                            }
                        }
                        ordereredTrials = channelTrialsFound.OrderBy(t => t.ChannelSubTrialBySubBloc[subBloc].InformationsByEvent[@event].Occurences.First().Code);
                        foreach (var channelTrial in channelTrialsNotFound)
                        {
                            ordereredTrials.Append(channelTrial);
                        }
                    }
                    else
                    {

                    }
                }
            }
            return ordereredTrials;
        }
        #endregion
    }
}