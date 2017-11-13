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
            NormalizedValuesBySite = valuesBySite.ToDictionary(valueBySite => valueBySite.Key, valueBySite => valueBySite.Value);
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
            // Maybe FIXME : awfull unmaintanable code
            Dictionary<Experience.Protocol.Event, List<int>> positionsByEvent = new Dictionary<Experience.Protocol.Event, List<int>>();
            Dictionary<string, float[][]> valuesBySite = new Dictionary<string, float[][]>();
            Dictionary<string, float[][]> baselineValuesBySite = new Dictionary<string, float[][]>();
            Dictionary<string, float[][]> normalizedValuesBySite = new Dictionary<string, float[][]>();
            int blocsLength = blocs.Length;
            Bloc bloc = blocs[0];
            foreach (var valueBySite in bloc.ValuesBySite)
            {
                int valuesLength = valueBySite.Value.Length;
                float[][] values = new float[valuesLength][];
                for (int j = 0; j < valuesLength; ++j)
                {
                    values[j] = new float[blocsLength];
                }
                valuesBySite.Add(valueBySite.Key, values);
            }
            foreach (var valueBySite in bloc.BaselineValuesBySite)
            {
                int valuesLength = valueBySite.Value.Length;
                float[][] values = new float[valuesLength][];
                for (int j = 0; j < valuesLength; ++j)
                {
                    values[j] = new float[blocsLength];
                }
                baselineValuesBySite.Add(valueBySite.Key, values);
            }
            foreach (var valueBySite in bloc.NormalizedValuesBySite)
            {
                int valuesLength = valueBySite.Value.Length;
                float[][] values = new float[valuesLength][];
                for (int j = 0; j < valuesLength; ++j)
                {
                    values[j] = new float[blocsLength];
                }
                normalizedValuesBySite.Add(valueBySite.Key, values);
            }
            for (int i = 0; i < blocsLength; ++i)
            {
                Bloc bloc2 = blocs[i];
                foreach (var valueBySite in bloc2.ValuesBySite)
                {
                    float[][] values = valuesBySite[valueBySite.Key];
                    int valuesLength = valueBySite.Value.Length;
                    for (int j = 0; j < valuesLength; ++j)
                    {
                        values[j][i] = valueBySite.Value[j];
                    }
                }
                foreach (var valueBySite in bloc2.BaselineValuesBySite)
                {
                    float[][] values = baselineValuesBySite[valueBySite.Key];
                    int valuesLength = valueBySite.Value.Length;
                    for (int j = 0; j < valuesLength; ++j)
                    {
                        values[j][i] = valueBySite.Value[j];
                    }
                }
                foreach (var valueBySite in bloc2.NormalizedValuesBySite)
                {
                    float[][] values = normalizedValuesBySite[valueBySite.Key];
                    int valuesLength = valueBySite.Value.Length;
                    for (int j = 0; j < valuesLength; ++j)
                    {
                        values[j][i] = valueBySite.Value[j];
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