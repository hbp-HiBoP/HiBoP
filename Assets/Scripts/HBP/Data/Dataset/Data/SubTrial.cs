using HBP.Data.Experience.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Experience.Dataset
{
    public class SubTrial
    {
        #region Properties
        public bool Found { get; set; }
        public Dictionary<Event, EventInformation> InformationsByEvent { get; set; }
        public Dictionary<string, string> UnitByChannel { get; set; }
        public Dictionary<string, float[]> RawValuesByChannel { get; set; }
        public Dictionary<string, float[]> BaselineValuesByChannel { get; set; }
        public Dictionary<string, float[]> ValuesByChannel { get; set; }
        #endregion

        #region Constructors
        public SubTrial(bool found) : this(new Dictionary<Event, EventInformation>(), new Dictionary<string, string>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>(), found)
        {
        }
        public SubTrial(Dictionary<Event, EventInformation> informationsByEvent, Dictionary<string, string> unitByChannel, Dictionary<string, float[]> rawValuesByChannel, Dictionary<string, float[]> baselineValuesByChannel, bool found)
        {
            UnitByChannel = unitByChannel;
            InformationsByEvent = informationsByEvent;
            RawValuesByChannel = rawValuesByChannel;
            BaselineValuesByChannel = baselineValuesByChannel;
            Found = found;
        }
        public SubTrial(Dictionary<string, float[]> valuesByChannel, Dictionary<string, string> unitByChannel, EventOccurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Core.Tools.Frequency frequency)
        {
            int startIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start);
            int endIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End);
            int baselineStartIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Baseline.Start);
            int baselineEndIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Baseline.End);
            if (startIndex >= 0)
            {
                RawValuesByChannel = EpochValues(valuesByChannel, startIndex, endIndex);
                BaselineValuesByChannel = EpochValues(valuesByChannel, baselineStartIndex, baselineEndIndex);
                UnitByChannel = unitByChannel;
                InformationsByEvent = FindEvents(mainEventOccurence, subBloc, occurencesByEvent, frequency);
                foreach (var treatment in subBloc.Treatments.OrderBy(t => t.Order))
                {
                    foreach (var channel in RawValuesByChannel.Keys.ToArray())
                    {
                        float[] rawValues = RawValuesByChannel[channel];
                        float[] baselineValues = BaselineValuesByChannel[channel];
                        treatment.Apply(ref rawValues, ref baselineValues, mainEventOccurence.Index - startIndex, mainEventOccurence.Index - baselineStartIndex, frequency);
                        RawValuesByChannel[channel] = rawValues; 
                    }
                }
                ValuesByChannel = RawValuesByChannel.ToDictionary(kv => kv.Key, kv => kv.Value.Clone() as float[]);
                Found = true;
            }
            else
            {
                RawValuesByChannel = new Dictionary<string, float[]>();
                UnitByChannel = new Dictionary<string, string>();
                ValuesByChannel = new Dictionary<string, float[]>();
                BaselineValuesByChannel = new Dictionary<string, float[]>();
                InformationsByEvent = new Dictionary<Event, EventInformation>();
                Found = false;
            }
        }
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (var eventInformation in InformationsByEvent.Values)
            {
                eventInformation.Clear();
            }
            InformationsByEvent.Clear();
            InformationsByEvent = new Dictionary<Event, EventInformation>();

            UnitByChannel.Clear();
            UnitByChannel = new Dictionary<string, string>();

            foreach (var channel in UnitByChannel.Keys)
            {
                RawValuesByChannel[channel] = new float[0];
                BaselineValuesByChannel[channel] = new float[0];
                ValuesByChannel[channel] = new float[0];
            }

            RawValuesByChannel.Clear();
            RawValuesByChannel = new Dictionary<string, float[]>();

            BaselineValuesByChannel.Clear();
            BaselineValuesByChannel = new Dictionary<string, float[]>();

            ValuesByChannel?.Clear();
            ValuesByChannel = new Dictionary<string, float[]>();
        }
        public void Normalize(float average, float standardDeviation)
        {
            foreach (var channel in RawValuesByChannel.Keys)
            {
                Normalize(average, standardDeviation, channel);
            }
        }
        public void Normalize(float average, float standardDeviation, string channel)
        {
            if (RawValuesByChannel.TryGetValue(channel, out float[] values))
            {
                values.Normalize(ValuesByChannel[channel], average, standardDeviation);
            }
        }
        #endregion

        #region Private Methods
        Dictionary<string, float[]> EpochValues(Dictionary<string, float[]> valuesByChannel, int startIndex, int endIndex)
        {
            // Initialize
            Dictionary<string, float[]> result = new Dictionary<string, float[]>(valuesByChannel.Count);

            // GetValues.
            int length = endIndex - startIndex + 1;
            foreach (var pair in valuesByChannel)
            {
                try
                {
                    float[] values = new float[length];
                    Array.Copy(pair.Value, startIndex, values, 0, length);
                    result.Add(pair.Key, values);
                }
                catch (Exception e)
                {
                    throw new CannotEpochAllTrialsException(e, pair.Value.Length - 1, startIndex, endIndex);
                }
            }
            return result;
        }
        Dictionary<Event,EventInformation> FindEvents(EventOccurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Core.Tools.Frequency frequency)
        {
            // Initialize
            Dictionary<Event, EventInformation> result = new Dictionary<Event, EventInformation>(subBloc.Events.Count);

            // Calculate start and end indexes of the window.
            int startIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start);
            int endIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End);

            EventInformation.EventOccurence mainOccurence = new EventInformation.EventOccurence(mainEventOccurence.Code, mainEventOccurence.Index, mainEventOccurence.Index - startIndex, 0, mainEventOccurence.Time, -subBloc.Window.Start, 0f); // Generate main event occurence.
            result.Add(subBloc.MainEvent, new EventInformation(new EventInformation.EventOccurence[] { mainOccurence }));

            foreach (var _event in subBloc.SecondaryEvents)
            {
                EventOccurence[] occurences = occurencesByEvent[_event].GetOccurences(startIndex, endIndex);
                List<EventInformation.EventOccurence> eventOccurences = new List<EventInformation.EventOccurence>(occurences.Length);
                foreach (var occurence in occurences)
                {
                    eventOccurences.Add(new EventInformation.EventOccurence(occurence.Code, occurence.Index, occurence.Index - startIndex, occurence.Index - mainOccurence.Index, occurence.Time, occurence.Time - mainOccurence.Time + mainOccurence.TimeFromStart, occurence.Time - mainOccurence.Time));
                }
                EventInformation eventInformation = new EventInformation(eventOccurences.ToArray());
                result.Add(_event, eventInformation);
            }
            return result;
        }
        #endregion
    }
}