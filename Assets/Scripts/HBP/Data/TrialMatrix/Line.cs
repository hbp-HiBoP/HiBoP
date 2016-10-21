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
            Event[] l_mainEventIndex = pos.ConvertEventCodeToSampleIndexAndCode(bloc.MainEvent.Codes.ToArray());

            int nbSecondaryEventsInBloc = bloc.SecondaryEvents.Count;
            Event[][] l_secondaryEventIndex = new Event[nbSecondaryEventsInBloc][];
            for (int i = 0; i < nbSecondaryEventsInBloc; i++)
            {
                Event[] l_secondaryEventIndexTemp = pos.ConvertEventCodeToSampleIndexAndCode(bloc.SecondaryEvents[i].Codes.ToArray());
                l_secondaryEventIndex[i] = l_secondaryEventIndexTemp;
            }

            // Calcul the size of a bloc
            int l_max = Mathf.FloorToInt((bloc.DisplayInformations.Window.y) * 0.001f * frequency);
            int l_BLmax = Mathf.FloorToInt((bloc.DisplayInformations.BaseLine.y) * 0.001f * frequency);
            int l_min = Mathf.CeilToInt((bloc.DisplayInformations.Window.x) * 0.001f * frequency);
            int l_BLmin = Mathf.CeilToInt((bloc.DisplayInformations.BaseLine.x) * 0.001f * frequency);
            int l_BLsize = l_BLmax - l_BLmin;
            int l_size = l_max - l_min;
            int l_mainEventPosition = -l_min;

            //  Initialize line list
            List<Line> l_lines = new List<Line>();

            //// Create and complete blocs
            int nbMainEvent = l_mainEventIndex.Length;
            for (int i = 0; i < nbMainEvent; i++)
            {
                // test if the bloc is in range
                int MainEventIndex = l_mainEventIndex[i].Position;

                int BLStartEventIndex = MainEventIndex + l_BLmin;
                int BLEndEventIndex = MainEventIndex + l_BLmax;

                int StartEventIndex = MainEventIndex + l_min;
                int EndEventIndex = MainEventIndex + l_max;

                int l_startIndex, l_endIndex;
                if (BLStartEventIndex > StartEventIndex)
                {
                    l_startIndex = StartEventIndex;
                }
                else
                {
                    l_startIndex = BLStartEventIndex;
                }
                if (BLEndEventIndex < EndEventIndex)
                {
                    l_endIndex = EndEventIndex;
                }
                else
                {
                    l_endIndex = BLEndEventIndex;
                }
                // Test if is in range
                if ((l_startIndex >= 0) && (l_endIndex < data.Length))
                {
                    //BaseLine
                    float[] l_baseLine = new float[l_BLsize];
                    for (int p = 0; p < l_BLsize; p++)
                    {
                        int l_index = p + MainEventIndex + l_BLmin;
                        l_baseLine[p] = data[l_index];
                    }

                    // Copy the data into the bloc
                    float[] l_dataTemps = new float[l_size];
                    for (int p = 0; p < l_size; p++)
                    {
                        int l_index = p + MainEventIndex + l_min;
                        l_dataTemps[p] = data[l_index];
                    }

                    // Find the index of everySecondaryEvent
                    Event[] l_secondaryEvents = new Event[nbSecondaryEventsInBloc];
                    for (int p = 0; p < nbSecondaryEventsInBloc; p++)
                    {
                        Event[] secondaryEvents = l_secondaryEventIndex[p];
                        bool l_found = false;
                        int position = -4;
                        int code = -5;
                        foreach (Event secondaryEvent in secondaryEvents)
                        {
                            if ((secondaryEvent.Position >= StartEventIndex) && (secondaryEvent.Position <= EndEventIndex))
                            {
                                position = secondaryEvent.Position - (MainEventIndex + l_min);
                                code = secondaryEvent.Code;
                                l_found = true;
                            }
                        }
                        if (!l_found)
                        {
                            position = -1;
                        }
                        l_secondaryEvents[p] = new Event(position, code);
                    }

                    // Add the bloc
                    Line line = new Line(l_dataTemps, l_baseLine, MainEventIndex, new Event(l_mainEventPosition, l_mainEventIndex[i].Code), l_secondaryEvents);
                    l_lines.Add(line);
                }
            }

            //Sort Lines
            return SortLines(bloc, l_lines.ToArray());
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
                            l_linesSorted = l_linesSorted.ThenBy(t => t.Main.Code);
                        }
                        else
                        {
                            if ((p - 1) < bloc.SecondaryEvents.Count)
                            {
                                l_linesSorted = l_linesSorted.ThenBy(t => t.Secondaries[p - 1].Code);
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