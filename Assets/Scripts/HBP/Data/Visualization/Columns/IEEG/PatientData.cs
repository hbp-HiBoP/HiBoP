using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Localizer;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Data.Visualization
{
    public class PatientData
    {
        #region Properties
        public Frequency Frequency { get; set; }
        public Dictionary<SubBloc, EventData> EventDataBySubBloc { get; set; }
        #endregion

        #region constructors
        public PatientData(EpochedData epochedData)
        {
            EventDataBySubBloc = new Dictionary<SubBloc, EventData>();
            foreach (var subBloc in epochedData.Trials[0].SubTrialBySubBloc.Keys)
            {
                SubTrial[] subTrials = new SubTrial[epochedData.Trials.Length];
                for (int i = 0; i < epochedData.Trials.Length; i++)
                {
                    subTrials[i] = epochedData.Trials[i].SubTrialBySubBloc[subBloc];
                }
                EventDataBySubBloc.Add(subBloc, new EventData(subTrials, subBloc));
            }
            Frequency = epochedData.Frequency;
        }
        public PatientData(Dictionary<SubBloc, EventData> eventDataBySubBloc, Frequency frequency)
        {
            EventDataBySubBloc = eventDataBySubBloc;
            Frequency = frequency;
        }
        public PatientData() : this(new Dictionary<SubBloc, EventData>(), new Frequency())
        {
        }
        #endregion
    }
}