using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.TrialMatrix
{
    public class Bloc
    {
        #region Properties
        public Experience.Protocol.Bloc ProtocolBloc { get; set; }
        public SubBloc[] SubBlocs { get; set; }
        #endregion

        #region Constructors
        public Bloc(Experience.Protocol.Bloc protocolBloc,SubBloc[] subBlocs)
        {
            ProtocolBloc = protocolBloc;
            SubBlocs = subBlocs;
        }
        public Bloc(Experience.Protocol.Bloc bloc, BlocChannelData blocChannelData)
        {
            List<SubBloc> subBlocs = new List<SubBloc>(bloc.SubBlocs.Count);
            IOrderedEnumerable<ChannelTrial> orderedTrials = SortTrials(bloc,blocChannelData.Trials);
            foreach (var subBloc in bloc.OrderedSubBlocs)
            {
                IEnumerable<SubTrial> subTrials = orderedTrials.Select(trial => new SubTrial(trial.ChannelSubTrialBySubBloc[subBloc]));
                SubBloc dataSubBloc = new SubBloc(subBloc, subTrials.ToArray());
                subBlocs.Add(dataSubBloc);
            }
            ProtocolBloc = bloc;
            SubBlocs = subBlocs.ToArray();
        }
        #endregion

        #region Private Methods
        static IOrderedEnumerable<ChannelTrial> SortTrials(Experience.Protocol.Bloc bloc, IEnumerable<ChannelTrial> trials)
        {
            // TODO
            IOrderedEnumerable<ChannelTrial> ordereredTrials = trials.OrderBy(t => t.IsValid);
            return ordereredTrials;
        }
        #endregion
    }
}