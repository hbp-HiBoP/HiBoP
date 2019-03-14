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
                IEnumerable<ChannelTrial> orderedTrials = blocChannelData.Trials.Where(t => t.IsValid); // FIXME : Ajouter la gestion des trials non complets.
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
    }
}