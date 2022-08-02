using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.TrialMatrix
{
    public class Bloc
    {
        #region Properties
        public Core.Data.Bloc ProtocolBloc { get; set; }
        public SubBloc[] SubBlocs { get; set; }
        #endregion

        #region Constructors
        public Bloc(Core.Data.Bloc protocolBloc,SubBloc[] subBlocs)
        {
            ProtocolBloc = protocolBloc;
            SubBlocs = subBlocs;
        }
        public Bloc(Core.Data.Bloc bloc, Core.Data.BlocChannelData blocChannelData)
        {
            List<SubBloc> subBlocs = new List<SubBloc>(bloc.SubBlocs.Count);
            IOrderedEnumerable<Core.Data.ChannelTrial> orderedTrials = SortTrials(bloc,blocChannelData.Trials.Where(t => t.IsValid)); // FIXME : Ajouter la gestion des trials non complets.
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
        static IOrderedEnumerable<Core.Data.ChannelTrial> SortTrials(Core.Data.Bloc bloc, IEnumerable<Core.Data.ChannelTrial> trials)
        {
            // TODO
            IOrderedEnumerable<Core.Data.ChannelTrial> ordereredTrials = trials.OrderBy(t => t.IsValid);
            return ordereredTrials;
        }
        #endregion
    }
}