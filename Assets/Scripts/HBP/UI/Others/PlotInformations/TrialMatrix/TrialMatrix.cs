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
        [SerializeField] GameObject m_LinePrefab;
        [SerializeField] Text m_TitleText;
        [SerializeField] ValuesLegend m_ValuesLegend;
        [SerializeField] TimeLegend m_TimeLegend;
        [SerializeField] RectTransform m_LinesRectTransform;

        Texture2D m_Colormap;

        d.TrialMatrix m_Data;
        public d.TrialMatrix Data
        {
            get { return m_Data; }
        }

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get { return m_Limits; }
            set
            {
                m_Limits = value;
                m_ValuesLegend.Set(m_Colormap, m_Limits, 5);
                foreach (Line line in Lines)
                {
                    foreach (Bloc bloc in line.Blocs)
                    {
                        bloc.UpdateLimits(m_Limits);
                    }
                }
            }
        }

        bool m_AutoLimits;
        public bool AutoLimits
        {
            get { return m_AutoLimits; }
            set { m_AutoLimits = value; }
        }

        List<Line> m_Lines = new List<Line>();
        public ReadOnlyCollection<Line> Lines
        {
            get { return new ReadOnlyCollection<Line>(m_Lines); }
        }

        public GenericEvent<Vector2> OnChangeLimits { get { return m_ValuesLegend.OnChangeLimits; } }
        public GenericEvent<bool> OnAutoLimits { get { return m_ValuesLegend.OnAutoLimits; } }
        #endregion

        #region Public Methods
        public void Set(d.TrialMatrix trialMatrix, bool autoLimits, Vector2 limits, Texture2D colormap)
        {
            DestroyImmediate(m_Colormap);
            m_Colormap = colormap;
            m_Data = trialMatrix;
            m_AutoLimits = autoLimits;
            if(autoLimits) Limits = trialMatrix.Limits;
            else Limits = limits;
            m_TitleText.text = trialMatrix.Title;

            //Organize array
            d.Bloc[] l_blocs = trialMatrix.Blocs.OrderBy(t => t.ProtocolBloc.Position.Row).ThenBy(t => t.ProtocolBloc.Position.Column).ToArray();

            // Set Legends
            SetLegends(Limits, trialMatrix.TimeLimitsByColumn);

            //Separate blocs by line
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

            //Generate Line
            for (int i = 0; i < l_lines.Count; i++)
            {
                AddLine(l_lines[i], maxBlocByRow, m_Colormap, Limits);
            }
            SelectAllLines();
        }
        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(Line line in this.m_Lines)
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
            m_TitleText = transform.Find("Title").Find("Text").GetComponent<Text>();
            m_ValuesLegend = transform.Find("Body").Find("ValuesLegend").GetComponent<ValuesLegend>();
            m_TimeLegend = transform.Find("Body").Find("Main").Find("TimeLegend").GetComponent<TimeLegend>();
            m_LinesRectTransform = transform.Find("Body").Find("Main").Find("Lines").GetComponent<RectTransform>();
        }
        void AddLine(d.Bloc[] blocsInLine,int max,Texture2D colorMap,Vector2 limits)
        {
            Line lines = (Instantiate(m_LinePrefab, m_LinesRectTransform) as GameObject).GetComponent<Line>();
            lines.Initialize();
            lines.Set(blocsInLine,max, colorMap,limits);
            this.m_Lines.Add(lines);
        }
        void SetLegends(Vector2 valueslimits,Vector2[] timeLimitsByColumn)
        {
            Profiler.BeginSample("set values");
            m_ValuesLegend.Set(m_Colormap, valueslimits,5);
            Profiler.EndSample();
            Profiler.BeginSample("set time");
            m_TimeLegend.Set(timeLimitsByColumn);
            Profiler.EndSample();
        }
        #endregion
    }
}