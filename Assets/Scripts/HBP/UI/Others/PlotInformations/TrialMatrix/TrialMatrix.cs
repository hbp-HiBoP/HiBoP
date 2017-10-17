using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using d = HBP.Data.TrialMatrix;
using UnityEngine.Profiling;

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
        }

        List<Line> lines = new List<Line>();
        public ReadOnlyCollection<Line> Lines
        {
            get { return new ReadOnlyCollection<Line>(lines); }
        }
        #endregion

        #region Public Methods
        public void Set(d.TrialMatrix trialMatrix)
        {
            data = trialMatrix;
            limits = trialMatrix.ValuesLimits;
            title.text = trialMatrix.Title;

            //Organize array
            Profiler.BeginSample("A");
            d.Bloc[] l_blocs = trialMatrix.Blocs.OrderBy(t => t.ProtocolBloc.DisplayInformations.Position.Row).ThenBy(t => t.ProtocolBloc.DisplayInformations.Position.Column).ToArray();
            Profiler.EndSample();

            // Set Legends
            Profiler.BeginSample("B");
            SetLegends(trialMatrix.ValuesLimits, trialMatrix.TimeLimitsByColumn);
            Profiler.EndSample();


            //Separate blocs by line
            Profiler.BeginSample("C");
            d.Bloc[][] l_lines = new d.Bloc[l_blocs[l_blocs.Length - 1].ProtocolBloc.DisplayInformations.Position.Row][];
            int l_blocsByRow = 0;
            for (int i = 0; i < l_lines.Length; i++)
            {
                l_lines[i] = System.Array.FindAll(l_blocs, x => x.ProtocolBloc.DisplayInformations.Position.Row == i + 1);
                if (l_blocsByRow < l_lines.Length) l_blocsByRow = l_lines.Length;
            }

            int maxBlocByRow = 0;
            foreach (d.Bloc[] line in l_lines)
            {
                int max = line[line.Length - 1].ProtocolBloc.DisplayInformations.Position.Column;
                if (max > maxBlocByRow)
                {
                    maxBlocByRow = max;
                }
            }
            Profiler.EndSample();

            //Generate Line
            Profiler.BeginSample("D");
            for (int i = 0; i < l_lines.Length; i++)
            {
                AddLine(l_lines[i], maxBlocByRow, colorMap, trialMatrix.ValuesLimits);
            }
            SelectAllLines();
            valuesLegend.onUpdateLimits.AddListener((l) => UpdateLimites(l));
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
            valuesLegend.Set(colorMap, valueslimits,5);
            timeLegend.Set(timeLimitsByColumn);
        }
        #endregion
    }
}