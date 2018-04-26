using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace HBP.Data.Experience
{
    /**
    * \class EpochedData
    * \author Adrien Gannerie
    * \version 1.0
    * \date 05 mai 2017
    * \brief Data epoched from a Data.
    */
    public class EpochedData
    {
        #region Properties
        public float Frequency { get; set; }
        public Localizer.Bloc[] Blocs { get; set; }
        public bool IsValid
        {
            get
            {
                return Blocs.Length > 0;
            }
        }
        #endregion

        #region Constructors
        public EpochedData(Protocol.Bloc bloc, Dataset.Data data)
        {
            // Find index for all the events of the blocs.
            Dictionary<Protocol.Event, int[]> indexByEvent = FindIndexByEvent(bloc.Events, data.POS);

            // Calcul number of samples before and after the main event.
            int numberOfSamplesBeforeMainEvent, numberOfSamplesAfterMainEvent;
            int BaselineNumberOfSamplesBeforeMainEvent, BaselineNumberOfSamplesAfterMainEvent;
            CalculateNumberOfSamples(bloc.Window, data.Frequency, out numberOfSamplesBeforeMainEvent, out numberOfSamplesAfterMainEvent);
            CalculateNumberOfSamples(bloc.Baseline, data.Frequency, out BaselineNumberOfSamplesBeforeMainEvent, out BaselineNumberOfSamplesAfterMainEvent);

            // Generate blocs.
            Blocs = (from index in indexByEvent[bloc.MainEvent] where (index + numberOfSamplesBeforeMainEvent >= 0 && index + numberOfSamplesAfterMainEvent < data.ValuesBySite.Values.First().Length)select new Localizer.Bloc(index + numberOfSamplesBeforeMainEvent, index + numberOfSamplesAfterMainEvent, index + BaselineNumberOfSamplesBeforeMainEvent, index + BaselineNumberOfSamplesAfterMainEvent, indexByEvent, data)).ToArray();
            Frequency = data.Frequency;
        }
        #endregion

        #region Private Methods
        Dictionary<Protocol.Event,int[]> FindIndexByEvent(IEnumerable<Protocol.Event> events, Localizer.POS pos)
        {
            return (from e in events select new KeyValuePair<Protocol.Event, int[]>(e, pos.GetIndexes(e.Codes).ToArray())).ToDictionary((pair) => pair.Key, (k) => k.Value);
        }
        void CalculateNumberOfSamples(Tools.CSharp.Window window, float frequency, out int numberOfSamplesBeforeMainEvent, out int numberOfSamplesAfterMainEvent)
        {
            numberOfSamplesBeforeMainEvent = Mathf.CeilToInt((window.Start) * 0.001f * frequency);
            numberOfSamplesAfterMainEvent = Mathf.FloorToInt((window.End) * 0.001f * frequency);
        }
        #endregion
    }
}
