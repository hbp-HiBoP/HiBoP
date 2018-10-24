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
        public SubTrial(bool found) : this(new Dictionary<Event, EventInformation>(), new Dictionary<string, float[]>(), new Dictionary<string, float[]>(), found) { }
        public SubTrial(Dictionary<Event, EventInformation> informationsByEvent, Dictionary<string, float[]> rawValuesByChannel, Dictionary<string, float[]> baselineValuesByChannel, bool found) : this()
        {
            InformationsByEvent = informationsByEvent;
            RawValuesByChannel = rawValuesByChannel;
            BaselineValuesByChannel = baselineValuesByChannel;
            Found = found;
        }
        public SubTrial(Dictionary<string, float[]> valuesByChannel, POS.Occurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Frequency frequency) : this()
        {
            RawValuesByChannel = EpochValues(valuesByChannel, mainEventOccurence.Index, subBloc.Window, frequency);
            ValuesByChannel = RawValuesByChannel.ToDictionary(kv => kv.Key, kv => kv.Value.Clone() as float[]);
            BaselineValuesByChannel = EpochValues(valuesByChannel, mainEventOccurence.Index, subBloc.Baseline, frequency);
            InformationsByEvent = FindEvents(mainEventOccurence,subBloc,occurencesByEvent, frequency);
            Found = true;
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
            if (ValuesByChannel.TryGetValue(channel, out values))
            {
                ValuesByChannel[channel] = values.Normalize(average, standardDeviation);
            }
        }
        #endregion

        #region Private Methods
        Dictionary<string,float[]> EpochValues(Dictionary<string,float[]> valuesByChannel,int mainEventIndex, Window window, Frequency frequency)
        {
            // Initialize
            Dictionary<string, float[]> result = new Dictionary<string, float[]>(valuesByChannel.Count);

            // Calculate start and end indexes of the window.
            int startIndex = mainEventIndex + frequency.ConvertToCeiledNumberOfSamples(window.Start);
            int endIndex = mainEventIndex + frequency.ConvertToFlooredNumberOfSamples(window.End);

            // GetValues.
            foreach (var pair in valuesByChannel)
            {
                float[] values = new float[endIndex - startIndex];
                Array.Copy(pair.Value, startIndex, values, 0, endIndex - startIndex);
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