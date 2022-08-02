using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using static HBP.Data.TrialMatrix.Grid.TrialMatrixGrid;

namespace HBP.Data.TrialMatrix.Grid
{
    public class ChannelBloc
    {
        #region Properties
        public bool IsFound { get; set; }
        public ChannelStruct Channel { get; set; }
        public Core.Data.Bloc Bloc { get; set; }
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
        public ChannelBloc(Core.Data.Bloc bloc, TrialMatrixData data, ChannelStruct channel)
        {
            Core.Data.DataInfo dataInfo = null;
            if(data is IEEGTrialMatrixData iEEGDataStruct)
            {
                dataInfo = iEEGDataStruct.Dataset.GetIEEGDataInfos().FirstOrDefault(d => d.Name == iEEGDataStruct.Name && d.Patient == channel.Patient);
            }
            else if(data is CCEPTrialMatrixData ccepDataStruct)
            {
                dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().FirstOrDefault(d => d.Name == ccepDataStruct.Name && d.Patient == channel.Patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel);
            }

            UnityEngine.Profiling.Profiler.BeginSample("GetData");
            Core.Data.BlocChannelData blocChannelData = Core.Data.DataManager.GetData(dataInfo, bloc, channel.Channel);
            UnityEngine.Profiling.Profiler.EndSample();

            IsFound = blocChannelData != null;
            Bloc = bloc;
            Channel = channel;
            if (IsFound)
            {
                List<SubBloc> subBlocs = new List<SubBloc>(bloc.SubBlocs.Count);
                IEnumerable<Core.Data.ChannelTrial> orderedTrials = blocChannelData.Trials.Where(t => t.IsValid); // FIXME : Ajouter la gestion des trials non complets.
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
        public void Standardize(Tuple<Tuple<Core.Data.Bloc,Core.Data.SubBloc>[],Tools.CSharp.Window>[] subBlocsAndWindowByColumn)
        {
            List<SubBloc> subBlocs = SubBlocs.ToList();
            for (int c = 0; c < subBlocsAndWindowByColumn.Length; c++)
            {
                Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], Tools.CSharp.Window> pair = subBlocsAndWindowByColumn[c];
                SubBloc subBloc = subBlocs.FirstOrDefault(s => pair.Item1.Any(v => v.Item2 == s.SubBlocProtocol));
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