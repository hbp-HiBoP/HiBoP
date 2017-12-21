using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using d = HBP.Data.TrialMatrix;
using UnityEngine.Profiling;
using UnityEngine.Events;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrix : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject linePrefab;
        [SerializeField]
        Texture2D colorMap;

        Text title;
        ValuesLegend valuesLegend;
        TimeLegend timeLegend;
        RectTransform linesRect;

        d.TrialMatrix data;
        public d.TrialMatrix Data
        {
            get { return data; }
        }

        Vector2 limits;
        public Vector2 Limits
        {
            get { return limits; }
            set { UpdateLimites(value); }
        }

        List<Line> lines = new List<Line>();
        public ReadOnlyCollection<Line> Lines
        {
            get { return new ReadOnlyCollection<Line>(lines); }
        }

        public GenericEvent<Vector2> OnChangeLimits { get { return valuesLegend.OnChangeLimits; } }
        public GenericEvent<bool> OnAutoLimits { get { return valuesLegend.OnAutoLimits; } }
        #endregion

        #region Public Methods
        public void Set(d.TrialMatrix trialMatrix)
        {
            data = trialMatrix;
            limits = trialMatrix.Limits;
            title.text = trialMatrix.Title;

            //Organize array
            Profiler.BeginSample("A");
            d.Bloc[] l_blocs = trialMatrix.Blocs.OrderBy(t => t.ProtocolBloc.Position.Row).ThenBy(t => t.ProtocolBloc.Position.Column).ToArray();
            Profiler.EndSample();

            // Set Legends
            Profiler.BeginSample("B");
            SetLegends(trialMatrix.Limits, trialMatrix.TimeLimitsByColumn);
            Profiler.EndSample();

            //Separate blocs by line
            Profiler.BeginSample("FindLine");
            List<d.Bloc[]> l_lines = new List<d.Bloc[]>();
            foreach (var bloc in l_blocs)
            {
                if (!l_lines.Exists((a) => a.Contains(bloc)))
                {
                    l_lines.Add(System.Array.FindAll(l_blocs, x => x.ProtocolBloc.Position.Row == bloc.ProtocolBloc.Position.Row));
                }
            }


            int maxBlocByRow = 0;
            foreach (d.Bloc[] line in l_lines)
            {
                    int max = line[line.Length - 1].ProtocolBloc.Position.Column;
                    if (max > maxBlocByRow)
                    {
                        maxBlocByRow = max;
                    }
            }
            Profiler.EndSample();

            //Generate Line
            Profiler.BeginSample("D");
            for (int i = 0; i < l_lines.Count; i++)
            {
                AddLine(l_lines[i], maxBlocByRow, colorMap, trialMatrix.Limits);
            }
            SelectAllLines();
            Profiler.EndSample();
        }
        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(Line line in this.lines)
            {
                line.SelectLines(lines, bloc, additive);
            }
        }
        public void SelectAllLines()
        {
            foreach(d.Bloc bloc in Data.Blocs)
            {
                int[] linesSelected = new int[bloc.Lines.Length];
                for (int i = 0; i < linesSelected.Length; i++)
                {
                    linesSelected[i] = i;
                }
                SelectLines(linesSelected, bloc.ProtocolBloc, true);
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            title = transform.Find("Title").Find("Text").GetComponent<Text>();
            valuesLegend = transform.Find("Body").Find("ValuesLegend").GetComponent<ValuesLegend>();
            timeLegend = transform.Find("Body").Find("Main").Find("TimeLegend").GetComponent<TimeLegend>();
            linesRect = transform.Find("Body").Find("Main").Find("Lines").GetComponent<RectTransform>();
        }
        void UpdateLimites(Vector2 limits)
        {
            this.limits = limits;

            // Set Legends
            valuesLegend.Set(colorMap, limits, 5);

            foreach (Line line in Lines)
            {
                foreach (Bloc bloc in line.Blocs)
                {
                    bloc.UpdateLimits(limits);
                }
            }
        }
        void AddLine(d.Bloc[] blocsInLine,int max,Texture2D colorMap,Vector2 limits)
        {
            Line lines = (Instantiate(linePrefab, linesRect) as GameObject).GetComponent<Line>();
            lines.Initialize();
            lines.Set(blocsInLine,max, colorMap,limits);
            this.lines.Add(lines);
        }
        void SetLegends(Vector2 valueslimits,Vector2[] timeLimitsByColumn)
        {
            Profiler.BeginSample("set values");
            valuesLegend.Set(colorMap, valueslimits,5);
            Profiler.EndSample();
            Profiler.BeginSample("set time");
            timeLegend.Set(timeLimitsByColumn);
            Profiler.EndSample();
        }
        #endregion
    }
}