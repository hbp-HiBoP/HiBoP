using UnityEngine;
using System;
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
            Dictionary<Protocol.Event, int[]> indexByEvent = new Dictionary<Protocol.Event, int[]>();
            indexByEvent.Add(bloc.MainEvent, data.POS.GetSamples(bloc.MainEvent.Codes).ToArray());
            foreach (Protocol.Event evnt in bloc.SecondaryEvents) indexByEvent.Add(evnt, data.POS.GetSamples(bloc.MainEvent.Codes).ToArray());

            // Calcul the size of a bloc and initialize bloc list
            int sampleAfterMainEvent = Mathf.FloorToInt((bloc.DisplayInformations.Window.End) * 0.001f * data.Frequency);
            int sampleBeforeMainEvent = Mathf.CeilToInt((bloc.DisplayInformations.Window.Start) * 0.001f * data.Frequency);
            int lenght = sampleAfterMainEvent - sampleBeforeMainEvent;
            List<Localizer.Bloc> blocs = new List<Localizer.Bloc>();

            foreach (int index in indexByEvent[bloc.MainEvent])
            {
                int firstIndex = index + sampleBeforeMainEvent;
                int lastIndex = index + sampleAfterMainEvent;
                if (firstIndex >= 0 && lastIndex < data.ValuesBySite[data.Patient.Brain.Implantation.Electrodes[0].Sites[0]].Length)
                {
                    Dictionary<Protocol.Event, int> positionByEvent = new Dictionary<Protocol.Event, int>();
                    foreach (var item in indexByEvent) positionByEvent.Add(item.Key, item.Value.DefaultIfEmpty(-1).First((t) => (t >= firstIndex && t <= lastIndex)));

                    Dictionary<Anatomy.Site, float[]> valuesBySite = new Dictionary<Anatomy.Site, float[]>();
                    foreach (var item in data.ValuesBySite)
                    {
                        float[] values = new float[lenght];
                        Array.Copy(item.Value, firstIndex, values, 0, lenght);
                        valuesBySite.Add(item.Key, values);
                    }
                }
            }
        }
        #endregion
    }
}
