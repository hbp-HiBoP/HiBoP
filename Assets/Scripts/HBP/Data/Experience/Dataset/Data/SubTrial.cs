using HBP.Data.Experience.Protocol;
using HBP.Data.Localizer;
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
        public Dictionary<string, float[]> RawValuesByChannel { get; set; }
        public Dictionary<string, float[]> BaselineValuesByChannel { get; set; }
        public Dictionary<string, float[]> ValuesByChannel { get; set; }
        #endregion

        #region Constructors
        public SubTrial(bool found) : this(new Dictionary<Event, EventInformation>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>(), found)
        {
        }
        public SubTrial(Dictionary<Event, EventInformation> informationsByEvent, Dictionary<string, float[]> rawValuesByChannel, Dictionary<string, float[]> baselineValuesByChannel, bool found)
        {
            InformationsByEvent = informationsByEvent;
            RawValuesByChannel = rawValuesByChannel;
            BaselineValuesByChannel = baselineValuesByChannel;
            Found = found;
        }
        public SubTrial(Dictionary<string, float[]> valuesByChannel, POS.Occurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Frequency frequency)
        {
            int startIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start);
            int endIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End);
            if (startIndex >= 0)
            {
                RawValuesByChannel = EpochValues(valuesByChannel, startIndex, endIndex);
                ValuesByChannel = RawValuesByChannel.ToDictionary(kv => kv.Key, kv => kv.Value.Clone() as float[]);
                BaselineValuesByChannel = EpochValues(valuesByChannel, startIndex, endIndex);
                InformationsByEvent = FindEvents(mainEventOccurence, subBloc, occurencesByEvent, frequency);
                Found = true;
            }
            else
            {
                RawValuesByChannel = new Dictionary<string, float[]>();
                ValuesByChannel = new Dictionary<string, float[]>();
                BaselineValuesByChannel = new Dictionary<string, float[]>();
                InformationsByEvent = new Dictionary<Event, EventInformation>();
                Found = false;
            }
        }
        #endregion

        #region Public Methods
        public void Normalize(float average, float standardDeviation)
        {
            foreach (var channel in RawValuesByChannel.Keys)
            {
                Normalize(average, standardDeviation, channel);
            }
        }
        public void Normalize(float average, float standardDeviation, string channel)
        {
            float[] values;
            if (RawValuesByChannel.TryGetValue(channel, out values))
            {
                values.Normalize(ValuesByChannel[channel], average, standardDeviation);
            }
        }
        #endregion

        #region Private Methods
        Dictionary<string,float[]> EpochValues(Dictionary<string,float[]> valuesByChannel, int startIndex, int endIndex)
        {
            // Initialize
            Dictionary<string, float[]> result = new Dictionary<string, float[]>(valuesByChannel.Count);

            // GetValues.
            int length = endIndex - startIndex + 1;
            foreach (var pair in valuesByChannel)
            {
                float[] values = new float[length];
                Array.Copy(pair.Value, startIndex, values, 0, length);
                result.Add(pair.Key, values);
            }
            return result;
        }
        Dictionary<Event,EventInformation> FindEvents(POS.Occurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Frequency frequency)
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
                POS.Occurence[] occurences = occurencesByEvent[_event].GetOccurences(startIndex, endIndex);
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