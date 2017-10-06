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
        public Localizer.Bloc[] Blocs { get; set; }
        #endregion

        #region Constructors
        public EpochedData(Protocol.Bloc bloc, Dataset.Data data)
        {
            // Find index for all the events of the blocs.
            Dictionary<Protocol.Event, int[]> indexByEvent = FindIndexByEvent(bloc.Events, data.POS);

            // Calcul number of samples before and after the main event.
            int numberOfSamplesBeforeMainEvent, numberOfSamplesAfterMainEvent;
            CalculateNumberOfSamples(bloc.DisplayInformations.Window, data.Frequency, out numberOfSamplesBeforeMainEvent, out numberOfSamplesAfterMainEvent);

            // Generate blocs.
            Blocs = (from index in indexByEvent[bloc.MainEvent] where (index + numberOfSamplesBeforeMainEvent >= 0 && index + numberOfSamplesAfterMainEvent < data.ValuesBySite.Values.First().Length)select new Localizer.Bloc(index + numberOfSamplesBeforeMainEvent, index + numberOfSamplesAfterMainEvent, indexByEvent, data)).ToArray();
        }
        #endregion

        #region Private Methods
        Dictionary<Protocol.Event,int[]> FindIndexByEvent(IEnumerable<Protocol.Event> events, Localizer.POS pos)
        {
            return (from e in events select new KeyValuePair<Protocol.Event, int[]>(e, pos.GetSamples(e.Codes).ToArray())).ToDictionary((pair) => pair.Key, (k) => k.Value);
        }
        void CalculateNumberOfSamples(Tools.CSharp.Window window, float frequency, out int numberOfSamplesBeforeMainEvent, out int numberOfSamplesAfterMainEvent)
        {
            numberOfSamplesBeforeMainEvent = Mathf.CeilToInt((window.Start) * 0.001f * frequency);
            numberOfSamplesAfterMainEvent = Mathf.FloorToInt((window.End) * 0.001f * frequency);
        }
        #endregion
    }
}
