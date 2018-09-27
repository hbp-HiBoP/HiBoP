using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using System;

namespace HBP.Data.Localizer
{
    public class SubBloc
    {
        #region Properties
        public bool Found { get; set; }
        public Dictionary<Experience.Protocol.Event, int> PositionByEvent { get; set; }
        public Dictionary<string, float[]> ValuesBySite { get; set; }
        public Dictionary<string, float[]> BaselineValuesBySite { get; set; }
        public Dictionary<string, float[]> NormalizedValuesBySite { get; set; }
        #endregion

        #region Constructors
        public SubBloc() : this(new Dictionary<Experience.Protocol.Event, int>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>())
        {
        }
        public SubBloc(Dictionary<Experience.Protocol.Event, int> positionByEvent, Dictionary<string, float[]> valuesBySite, Dictionary<string, float[]> baselineValuesBySite, Dictionary<string, float[]> normalizedValuesBySite)
        {
            PositionByEvent = positionByEvent;
            ValuesBySite = valuesBySite;
            BaselineValuesBySite = baselineValuesBySite;
            NormalizedValuesBySite = normalizedValuesBySite;
        }
        public SubBloc(int firstIndex, int lastIndex, int BaselineFirstIndex, int BaselineLastIndex, Dictionary<Experience.Protocol.Event, int[]> indexByEvent, Experience.Dataset.Data data)
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
        #endregion

        #region Public static Methods
        public static SubBloc Average(SubBloc[] subBlocs, Enums.AveragingType valueAveragingMode, Enums.AveragingType eventPositionAveragingMode)
        {
            // Initialization Dictionary.
            SubBloc subBloc = subBlocs.FirstOrDefault((s) => s.Found);
            if (subBloc == null) return new SubBloc();

            int subBlocsLength = subBlocs.Length;
            int valuesLength = subBloc.ValuesBySite.First().Value.Length;
            int baselineLength = subBloc.BaselineValuesBySite.First().Value.Length;
            int siteLength = subBloc.BaselineValuesBySite.Count;
            Dictionary<Experience.Protocol.Event, int[]> positionsByEvent = new Dictionary<Experience.Protocol.Event, int[]>();
            string[] sites = new string[siteLength];
            float[][][] valuesBySite = new float[siteLength][][];
            float[][][] baselineValuesBySite = new float[siteLength][][];
            float[][][] normalizedValuesBySite = new float[siteLength][][];
            foreach (var _event in subBloc.PositionByEvent.Keys)
            {
                positionsByEvent.Add(_event, new int[subBlocsLength]);
            }
            sites = subBloc.ValuesBySite.Keys.ToArray();
            float[][] values;
            for (int s = 0; s < siteLength; ++s)
            {
                values = new float[valuesLength][];
                for (int j = 0; j < valuesLength; ++j)
                {
                    values[j] = new float[subBlocsLength];
                }
                valuesBySite[s] = values;

                values = new float[baselineLength][];
                for (int j = 0; j < baselineLength; ++j)
                {
                    values[j] = new float[subBlocsLength];
                }
                baselineValuesBySite[s] = values;

                values = new float[valuesLength][];
                for (int j = 0; j < valuesLength; ++j)
                {
                    values[j] = new float[subBlocsLength];
                }
                normalizedValuesBySite[s] = values;
            }
            // Fill Dictionary.
            float[] siteValues;
            for (int b = 0; b < subBlocsLength; ++b)
            {
                subBloc = subBlocs[b];
                if (!subBloc.Found) continue;
                foreach (var _event in subBloc.PositionByEvent)
                {
                    positionsByEvent[_event.Key][b] = _event.Value;
                }
                for (int s = 0; s < siteLength; ++s)
                {
                    values = valuesBySite[s];
                    siteValues = subBloc.ValuesBySite[sites[s]];
                    for (int v = 0; v < valuesLength; v++)
                    {
                        values[v][b] = siteValues[v];
                    }

                    values = baselineValuesBySite[s];
                    siteValues = subBloc.BaselineValuesBySite[sites[s]];
                    for (int v = 0; v < baselineLength; v++)
                    {
                        values[v][b] = siteValues[v];
                    }

                    values = normalizedValuesBySite[s];
                    siteValues = subBloc.NormalizedValuesBySite[sites[s]];
                    for (int v = 0; v < valuesLength; v++)
                    {
                        values[v][b] = siteValues[v];
                    }
                }
            }
            // Compute averaging.
            SubBloc result = new SubBloc();
            switch (eventPositionAveragingMode)
            {
                case Data.Enums.AveragingType.Mean:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, UnityEngine.Mathf.RoundToInt(item.Value.Mean()));
                    break;
                case Data.Enums.AveragingType.Median:
                    foreach (var item in positionsByEvent) result.PositionByEvent.Add(item.Key, item.Value.Median());
                    break;
            }
            switch (valueAveragingMode)
            {
                case Data.Enums.AveragingType.Mean:
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = valuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Mean();
                        }
                        result.ValuesBySite.Add(sites[s], val);
                    }
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = baselineValuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Mean();
                        }
                        result.BaselineValuesBySite.Add(sites[s], val);
                    }
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = normalizedValuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Mean();
                        }
                        result.NormalizedValuesBySite.Add(sites[s], val);
                    }
                    break;
                case Data.Enums.AveragingType.Median:
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = valuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Median();
                        }
                        result.ValuesBySite.Add(sites[s], val);
                    }
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = baselineValuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Median();
                        }
                        result.BaselineValuesBySite.Add(sites[s], val);
                    }
                    for (int s = 0; s < siteLength; s++)
                    {
                        float[][] valbysite = normalizedValuesBySite[s];
                        float[] val = new float[valbysite.Length];
                        for (int b = 0; b < valbysite.Length; ++b)
                        {
                            val[b] = valbysite[b].Median();
                        }
                        result.NormalizedValuesBySite.Add(sites[s], val);
                    }
                    break;
            }
            return result;
        }
        #endregion
    }
}