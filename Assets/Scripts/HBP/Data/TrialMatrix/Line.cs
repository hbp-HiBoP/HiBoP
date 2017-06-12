using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Tools.CSharp;

namespace HBP.Data.TrialMatrix
{
    public class Line
    {
        #region Attributs
        public float[] Data;
        public float[] DataWithCorrection;
        public float[] BaseLine;
        public int Position;
        public Event Main;
        public Event[] Secondaries;
        #endregion

        #region Constructor
        public Line(float[] data, float[] baseLine, int position, Event main, Event[] secondaries)
        {
            Data = data;
            BaseLine = baseLine;
            Position = position;
            Main = main;
            Secondaries = secondaries;
        }
        #endregion

        #region Public Methods
        public static Line[] MakeLines(Experience.Protocol.Bloc bloc, Localizer.POS pos, float[] data, float frequency)
        {
            // Read Index
            Event[] mainEvent = (from sample in pos.GetSamples(bloc.MainEvent.Codes) select new Event(sample,bloc.MainEvent)).ToArray();
            Event[][] secondaryEventsByType = new Event[bloc.SecondaryEvents.Count][];
            for (int i = 0; i < bloc.SecondaryEvents.Count; i++)
            {
                Event[] secondaryEvent = (from sample in pos.GetSamples(bloc.SecondaryEvents[i].Codes) select new Event(sample, bloc.SecondaryEvents[i])).ToArray();
                secondaryEventsByType[i] = secondaryEvent;
            }

            // Calcul the size of a bloc
            int windowAfter = Mathf.FloorToInt((bloc.DisplayInformations.Window.End) * 0.001f * frequency);
            int baselineAfter = Mathf.FloorToInt((bloc.DisplayInformations.BaseLine.End) * 0.001f * frequency);
            int windowBefore = Mathf.CeilToInt((bloc.DisplayInformations.Window.Start) * 0.001f * frequency);
            int baselineBefore = Mathf.CeilToInt((bloc.DisplayInformations.BaseLine.Start) * 0.001f * frequency);
            int baseLineSize = baselineAfter - baselineBefore;
            int windowSize = windowAfter - windowBefore;
            int mainEventPosition = -windowBefore;

            //  Initialize line list
            List<Line> lines = new List<Line>();

            //// Create and complete blocs
            for (int i = 0; i < mainEvent.Length; i++)
            {
                // test if the bloc is in range
                int MainEventIndex = mainEvent[i].Position;

                int baselineFirstIndex = MainEventIndex + baselineBefore;
                int baselineLastIndex = MainEventIndex + baselineAfter;

                int windowFirstIndex = MainEventIndex + windowBefore;
                int windowLastIndex = MainEventIndex + windowAfter;

                int firstIndex, lastIndex;
                if (baselineFirstIndex > windowFirstIndex) firstIndex = windowFirstIndex;
                else firstIndex = baselineFirstIndex;

                if (baselineLastIndex < windowLastIndex) lastIndex = windowLastIndex;
                else lastIndex = baselineLastIndex;

                // Test if is in range
                if ((firstIndex >= 0) && (lastIndex < data.Length))
                {
                    //BaseLine
                    float[] baseLine = new float[baseLineSize];
                    for (int p = 0; p < baseLineSize; p++)
                    {
                        int index = p + MainEventIndex + baselineBefore;
                        baseLine[p] = data[index];
                    }

                    // Copy the data into the bloc
                    float[] window = new float[windowSize];
                    for (int p = 0; p < windowSize; p++)
                    {
                        int index = p + MainEventIndex + windowBefore;
                        window[p] = data[index];
                    }

                    // Find the index of everySecondaryEvent
                    Event[] secondaryEventsOfTheLine = new Event[bloc.SecondaryEvents.Count];
                    for (int p = 0; p < bloc.SecondaryEvents.Count; p++)
                    {
                        Event[] secondaryEvents = secondaryEventsByType[p];
                        bool found = false;
                        int position = int.MinValue;
                        int code = int.MinValue;
                        foreach (Event secondaryEvent in secondaryEvents)
                        {
                            if ((secondaryEvent.Position >= windowFirstIndex) && (secondaryEvent.Position <= windowLastIndex))
                            {
                                position = secondaryEvent.Position - (MainEventIndex + windowBefore);
                                code = secondaryEvent.ProtocolEvent.Codes[0];
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            position = -1;
                        }
                        // TODO
                        secondaryEventsOfTheLine[p] = new Event(position, new Experience.Protocol.Event());
                    }

                    // Add the bloc
                    Line line = new Line(window, baseLine, MainEventIndex, new Event(mainEventPosition, mainEvent[i].ProtocolEvent), secondaryEventsOfTheLine);
                    lines.Add(line);
                }
            }

            //Sort Lines
            return SortLines(bloc, lines.ToArray());
        }

        public void CorrectionByBaseLine(float average,float standardDeviation)
        {
            DataWithCorrection = new float[Data.Length];
            for (int i = 0; i < Data.Length; i++)
            {
                DataWithCorrection[i] = (Data[i] - average) / standardDeviation;
            }
        }
        #endregion

        #region Private Methods
        static Line[] SortLines(Experience.Protocol.Bloc bloc, Line[] line)
        {
            string l_sort = bloc.DisplayInformations.Sort;
            string[] l_sortCommands = l_sort.SplitInParts(2).ToArray();
            IOrderedEnumerable<Line> l_linesSorted = line.OrderBy(x => 1);
            for (int i = 0; i < l_sortCommands.Length; i++)
            {
                string l_sortCommand = l_sortCommands[i];
                if (l_sortCommand.Length == 2)
                {
                    int p = int.Parse(l_sortCommand[1].ToString());

                    if (l_sortCommand[0] == 'C')
                    {
                        if (p == 0)
                        {
                            // TODO.
                            l_linesSorted = l_linesSorted.ThenBy(t => t.Main.ProtocolEvent.Codes[0]);
                        }
                        else
                        {
                            if ((p - 1) < bloc.SecondaryEvents.Count)
                            {
                                // TODO.
                                l_linesSorted = l_linesSorted.ThenBy(t => t.Secondaries[p - 1].ProtocolEvent.Codes[0]);
                            }
                        }
                    }
                    else if (l_sortCommand[0] == 'L')
                    {
                        if (p == 0)
                        {
                            l_linesSorted = l_linesSorted.ThenBy(t => t.Main.Position);
                        }
                        else
                        {
                            if ((p - 1) < bloc.SecondaryEvents.Count)
                            {
                                l_linesSorted = l_linesSorted.ThenBy(t => t.Secondaries[p - 1].Position);
                            }
                        }
                    }
                }
            }
            return l_linesSorted.ToArray();
        }
        #endregion
    }
}