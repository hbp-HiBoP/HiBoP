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
        public Dictionary<string, float[]> ValuesBySite { get; set; }
        public Dictionary<string, float[]> BaselineValuesBySite { get; set; }
        public Dictionary<string, float[]> NormalizedValuesBySite { get; set; }
        #endregion

        #region Constructor
        public Bloc(Dictionary<Experience.Protocol.Event, int> positionByEvent, Dictionary<string, float[]> valuesBySite, Dictionary<string, float[]> baselineValuesBySite,Dictionary<string,float[]> normalizedValuesBySite)
		{
            PositionByEvent = positionByEvent;
            ValuesBySite = valuesBySite;
            BaselineValuesBySite = baselineValuesBySite;
            NormalizedValuesBySite = normalizedValuesBySite;
        }
        public Bloc(int firstIndex, int lastIndex, int BaselineFirstIndex, int BaselineLastIndex, Dictionary<Experience.Protocol.Event, int[]> indexByEvent, Experience.Dataset.Data data)
        {
            int lenght = lastIndex - firstIndex + 1;
            int BaselineLenght = BaselineLastIndex - BaselineFirstIndex + 1;
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

            Dictionary<string, float[]> baselineValuesBySite = new Dictionary<string, float[]>();
            foreach (var pair in data.ValuesBySite)
            {
                float[] values = new float[BaselineLenght];
                Array.Copy(pair.Value, BaselineFirstIndex, values, 0, BaselineLenght);
                baselineValuesBySite.Add(pair.Key, values);
            }
            BaselineValuesBySite = baselineValuesBySite;
            NormalizedValuesBySite = new Dictionary<string, float[]>();
        }
        public Bloc(): this (new Dictionary<Experience.Protocol.Event, int>(),new Dictionary<string, float[]>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>())
        {
        }
        #endregion

        #region Public Methods
        public void Normalize(float average, float standardDeviation, string siteToNormalize = "")
        {
            if (string.IsNullOrEmpty(siteToNormalize))
            {
                NormalizedValuesBySite = (from site in ValuesBySite.Keys select new KeyValuePair<string, float[]>(site, (from value in ValuesBySite[site] select (value - average) / standardDeviation).ToArray())).ToDictionary(p => p.Key, p => p.Value);
            }
            else
            {
                NormalizedValuesBySite[siteToNormalize] = (from value in ValuesBySite[siteToNormalize] select (value - average) / standardDeviation).ToArray();
            }
        }
        #endregion

        #region Public static Methods
        public static Bloc Average(Bloc[] blocs, Settings.GeneralSettings.AveragingMode valueAveragingMode, Settings.GeneralSettings.AveragingMode eventPositionAveragingMode )
        {
            Dictionary<Experience.Protocol.Event, List<int>> positionsByEvent = new Dictionary<Experience.Protocol.Event, List<int>>();
            Dictionary<string, List<float>[]> valuesBySite = new Dictionary<string, List<float>[]>();
            Dictionary<string, List<float>[]> baselineValuesBySite = new Dictionary<string, List<float>[]>();
            Dictionary<string, List<float>[]> normalizedValuesBySite = new Dictionary<string, List<float>[]>();
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

                    if (!baselineValuesBySite.ContainsKey(site)) baselineValuesBySite.Add(site, new List<float>[bloc.BaselineValuesBySite[site].Length]);
                    for (int v = 0; v < baselineValuesBySite[site].Length; v++)
                    {
                        if (baselineValuesBySite[site][v] == null) baselineValuesBySite[site][v] = new List<float>();
                        baselineValuesBySite[site][v].Add(bloc.BaselineValuesBySite[site][v]);
                    }

                    if (!normalizedValuesBySite.ContainsKey(site)) normalizedValuesBySite.Add(site, new List<float>[bloc.NormalizedValuesBySite[site].Length]);
                    for (int v = 0; v < normalizedValuesBySite[site].Length; v++)
                    {
                        if (normalizedValuesBySite[site][v] == null) normalizedValuesBySite[site][v] = new List<float>();
                        normalizedValuesBySite[site][v].Add(bloc.NormalizedValuesBySite[site][v]);
                    }
                }
            }

            Bloc result = new Bloc();
            switch (eventPositionAveragingMode)
            {
                case Settings.GeneralSettings.AveragingMode.Mean:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, UnityEngine.Mathf.RoundToInt((float) item.Value.Average()));
                    break;
                case Settings.GeneralSettings.AveragingMode.Median:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, item.Value.Median());
                    break;
            }
            switch (valueAveragingMode)
            {
                case Settings.GeneralSettings.AveragingMode.Mean:
                    foreach (var item in valuesBySite) result.ValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Average()).ToArray());
                    foreach (var item in baselineValuesBySite) result.BaselineValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Average()).ToArray());
                    foreach (var item in normalizedValuesBySite) result.NormalizedValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Average()).ToArray());
                    break;
                case Settings.GeneralSettings.AveragingMode.Median:
                    foreach (var item in valuesBySite) result.ValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Median()).ToArray());
                    foreach (var item in baselineValuesBySite) result.BaselineValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Median()).ToArray());
                    foreach (var item in normalizedValuesBySite) result.NormalizedValuesBySite.Add(item.Key, (from elmt in item.Value select elmt.Median()).ToArray());
                    break;
            }
            return result;		
		}
        #endregion
    }
} 