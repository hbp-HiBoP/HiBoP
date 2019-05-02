using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public class PatientData
    {
        #region Properties
        public Tools.CSharp.EEG.Frequency Frequency { get; set; }
        public Dictionary<SubBloc, SubBlocEventsStatistics> EventDataBySubBloc { get; set; }
        #endregion

        #region constructors
        public PatientData(BlocData blocData)
        {
            //EventDataBySubBloc = new Dictionary<SubBloc, SubBlocEventsStatistics>();
            //foreach (var subBloc in blocData.Trials[0].SubTrialBySubBloc.Keys)
            //{
            //    SubTrial[] subTrials = new SubTrial[blocData.Trials.Length];
            //    for (int i = 0; i < blocData.Trials.Length; i++)
            //    {
            //        subTrials[i] = blocData.Trials[i].SubTrialBySubBloc[subBloc];
            //    }
            //    EventDataBySubBloc.Add(subBloc, new SubBlocEventsStatistics(subTrials, subBloc));
            //}
            //Frequency = blocData.Frequency;
        }
        public PatientData(Dictionary<SubBloc, SubBlocEventsStatistics> eventDataBySubBloc, Tools.CSharp.EEG.Frequency frequency)
        {
            EventDataBySubBloc = eventDataBySubBloc;
            Frequency = frequency;
        }
        public PatientData() : this(new Dictionary<SubBloc, SubBlocEventsStatistics>(), new Tools.CSharp.EEG.Frequency())
        {
        }
        #endregion
    }
}