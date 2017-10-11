using System;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.Localizer
{
    public class Bloc
	{
        #region Properties
        public Dictionary<Experience.Protocol.Event, int> PositionByEvent;
        public Dictionary<string, float[]> ValuesBySite;
        #endregion

        #region Constructor
        public Bloc(Dictionary<Experience.Protocol.Event,int> positionByEvent,Dictionary<string,float[]> valuesBySite)
		{
            PositionByEvent = positionByEvent;
            ValuesBySite = valuesBySite;
		}
        public Bloc(int firstIndex, int lastIndex, Dictionary<Experience.Protocol.Event, int[]> indexByEvent, Experience.Dataset.Data data)
        {
            int lenght = lastIndex - firstIndex + 1;
            Dictionary<Experience.Protocol.Event, int> positionByEvent = new Dictionary<Experience.Protocol.Event, int>();
            foreach (var pair in indexByEvent)
            {
                int eventIndex = pair.Value.DefaultIfEmpty(-1).FirstOrDefault((t) => (t >= firstIndex && t <= lastIndex));
                if (eventIndex != -1)
                {
                    eventIndex -= firstIndex;
                }
                positionByEvent.Add(pair.Key, eventIndex);
            }
            PositionByEvent = positionByEvent;

            Dictionary<string, float[]> valuesBySite = new Dictionary<string, float[]>();
            foreach (var pair in data.ValuesBySite)
            {
                float[] values = new float[lenght];
                Array.Copy(pair.Value, firstIndex, values, 0, lenght);
                valuesBySite.Add(pair.Key, values);
            }
            ValuesBySite = valuesBySite;
        }
        public Bloc(): this (new Dictionary<Experience.Protocol.Event, int>(),new Dictionary<string, float[]>())
        {
        }
        #endregion

        #region Public static Methods
        public static Bloc Average(Bloc[] blocs)
        {
            Dictionary<Experience.Protocol.Event, List<int>> positionsByEvent = new Dictionary<Experience.Protocol.Event, List<int>>();
            Dictionary<string, List<float>[]> valuesBySite = new Dictionary<string, List<float>[]>();

            foreach (Bloc bloc in blocs)
            {
                foreach (Experience.Protocol.Event _event in bloc.PositionByEvent.Keys)
                {
                    if (!positionsByEvent.ContainsKey(_event)) positionsByEvent.Add(_event, new List<int>());
                    positionsByEvent[_event].Add(bloc.PositionByEvent[_event]);
                }
                foreach (string site in bloc.ValuesBySite.Keys)
                {
                    if (!valuesBySite.ContainsKey(site)) valuesBySite.Add(site, new List<float>[bloc.ValuesBySite[site].Length]);
                    for (int v = 0; v < valuesBySite[site].Length; v++)
                    {
                        if (valuesBySite[site][v] == null) valuesBySite[site][v] = new List<float>();
                        valuesBySite[site][v].Add(bloc.ValuesBySite[site][v]);
                    }
                }
            }

            Bloc result = new Bloc();
            switch (ApplicationState.GeneralSettings.EventPositionAveraging)
            {
                case Settings.GeneralSettings.AveragingMode.Mean:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, UnityEngine.Mathf.RoundToInt((float) item.Value.Average()));
                    break;
                case Settings.GeneralSettings.AveragingMode.Median:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, item.Value.Median());
                    break;
            }
            switch (ApplicationState.GeneralSettings.ValueAveraging)
            {
                case Settings.GeneralSettings.AveragingMode.Mean:
                    foreach (var item in valuesBySite) result.ValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Average()).ToArray());
                    break;
                case Settings.GeneralSettings.AveragingMode.Median:
                    foreach (var item in valuesBySite) result.ValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Median()).ToArray());
                    break;
            }
            return result;		
		}
		#endregion
	}
} 