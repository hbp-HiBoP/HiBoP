using System.Linq;
using System.Collections.Generic;

namespace HBP.Core.Data
{
    public struct Trial
    {
        #region Properties
        public bool IsValid
        {
            get
            {
                return SubTrialBySubBloc.Values.All(sb => sb.Found);
            }
        }
        public Dictionary<SubBloc, SubTrial> SubTrialBySubBloc { get; set; }
        #endregion

        #region Constructor
        public Trial(Dictionary<SubBloc, SubTrial> subTrialBySubBloc)
        {
            SubTrialBySubBloc = subTrialBySubBloc;
        }
        public Trial(Dictionary<string,float[]> valuesByChannel, Dictionary<string, string> unitByChannel,  int startIndex, EventOccurence mainEventOccurence, int endIndex, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Bloc bloc, Core.Tools.Frequency frequency) : this()
        {
            SubTrialBySubBloc = new Dictionary<SubBloc, SubTrial>(bloc.SubBlocs.Count); // Initialize dictionary

            List<SubBloc> orderedSubBlocs = bloc.SubBlocs.OrderBy(sb => sb.Order).ThenBy(sb => sb.Name).ToList(); // Order sub blocs.
            int mainSubBlocIndex = orderedSubBlocs.IndexOf(bloc.MainSubBloc); // Find main sub bloc index.

            // Generate main Sub Trial
            SubTrial mainSubTrial = new SubTrial(valuesByChannel, unitByChannel, mainEventOccurence, bloc.MainSubBloc, occurencesByEvent, frequency);
            SubTrialBySubBloc.Add(bloc.MainSubBloc, mainSubTrial);

            // Research before.
            int start = startIndex, end = mainEventOccurence.Index;
            for (int i = mainSubBlocIndex - 1; i >= 0; i--)
            {
                SubBloc subBloc = orderedSubBlocs[i];
                EventOccurence[] occurences = occurencesByEvent[subBloc.MainEvent].GetOccurences(start, end).OrderBy(o => o.Index).ToArray();
                SubTrial subTrial;
                if (occurences.Length > 0)
                {
                    EventOccurence mainEventOccurenceOfSecondaryBloc = occurences.LastOrDefault();
                    subTrial = new SubTrial(valuesByChannel, unitByChannel, mainEventOccurenceOfSecondaryBloc, subBloc, occurencesByEvent, frequency);
                }
                else
                {
                    subTrial = new SubTrial(false);
                }
                SubTrialBySubBloc.Add(subBloc, subTrial);
                if (subTrial.Found) end = subTrial.InformationsByEvent[subBloc.MainEvent].Occurences[0].Index;
            }

            // Research after.
            start = mainEventOccurence.Index;
            end = endIndex;
            for (int i = mainSubBlocIndex + 1; i < orderedSubBlocs.Count; i++)
            {
                SubBloc subBloc = orderedSubBlocs[i];
                EventOccurence[] occurences = occurencesByEvent[subBloc.MainEvent].GetOccurences(start, end).OrderBy(o => o.Index).ToArray();
                SubTrial subTrial;
                if (occurences.Length > 0)
                {
                    EventOccurence mainEventOccurenceOfSecondaryBloc = occurences.FirstOrDefault();
                    subTrial = new SubTrial(valuesByChannel, unitByChannel, mainEventOccurenceOfSecondaryBloc, subBloc, occurencesByEvent, frequency);
                }
                else
                {
                    subTrial = new SubTrial(false);
                }
                SubTrialBySubBloc.Add(subBloc, subTrial);
                if (subTrial.Found) start = subTrial.InformationsByEvent[subBloc.MainEvent].Occurences[0].Index;
            }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var subTrial in SubTrialBySubBloc.Values)
            {
                subTrial.Clear();
            }
            SubTrialBySubBloc.Clear();
            SubTrialBySubBloc = new Dictionary<SubBloc, SubTrial>();
        }
        #endregion
    }
}