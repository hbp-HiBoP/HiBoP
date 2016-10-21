using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using d = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrix : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject m_linePrefab;

        [SerializeField]
        UnityEngine.UI.LayoutElement m_layout;

        [SerializeField]
        Value m_value;

        [SerializeField]
        TimeBlocs m_timeBlocs;

        [SerializeField]
        Texture2D m_colorMap;

        [SerializeField]
        UnityEngine.UI.Text m_title;

        [SerializeField]
        GameObject m_window;

        public d.TrialMatrix TrialMatrixData;
        public Vector2 Limits;

        List<Line> m_lines = new List<Line>();
        public Line[] Lines { get { return m_lines.ToArray(); } }

        int m_maxBlocsByLine = 0;
        float m_ratio = 0.4f;
        #endregion

        #region Public Methods
        public void Set(d.TrialMatrix trialMatrix)
        {
            TrialMatrixData = trialMatrix;
            Limits = trialMatrix.ValuesLimits;
            m_title.text = trialMatrix.Title;

            //Organize array
            d.Bloc[] l_blocs = trialMatrix.Blocs.OrderBy(t => t.PBloc.DisplayInformations.Row).ThenBy(t => t.PBloc.DisplayInformations.Column).ToArray();

            // Set Legends
            SetLegends(trialMatrix.ValuesLimits, trialMatrix.TimeLimitsByColumn);


            //Separate blocs by line
            d.Bloc[][] l_lines = new d.Bloc[l_blocs[l_blocs.Length - 1].PBloc.DisplayInformations.Row][];
            int l_blocsByRow = 0;
            for (int i = 0; i < l_lines.Length; i++)
            {
                l_lines[i] = System.Array.FindAll(l_blocs, x => x.PBloc.DisplayInformations.Row == i+1);
                if(l_blocsByRow < l_lines.Length)
                {
                    l_blocsByRow = l_lines.Length;
                }
            }

            int maxBlocByRow =0;
            foreach(d.Bloc[] line in l_lines)
            {
                int max = line[line.Length - 1].PBloc.DisplayInformations.Column;
                if (max > maxBlocByRow)
                {
                    maxBlocByRow = max;
                }
            }
            //Generate Line
            for (int i = 0; i < l_lines.Length; i++)
            {
                AddLine(l_lines[i],maxBlocByRow, m_colorMap, trialMatrix.ValuesLimits);
            }
            m_maxBlocsByLine = maxBlocByRow;
            CalculateSize();
            SelectAllLines();
        }

        public void UpdateLimites(Vector2 limits)
        {
            Limits = limits;
            // Set Legends
            m_value.Set(m_colorMap, limits.x, limits.y, 5);

            foreach(Line line in Lines)
            {
                foreach(Bloc bloc in line.Blocs)
                {
                    bloc.UpdateLimits(limits);
                }
            }
        }

        public void OpenWindow()
        {
            m_window.SetActive(true);
        }

        public void CloseWindow()
        {
            m_window.SetActive(false);
        }

        public void SelectLines(int[] lines, Data.Experience.Protocol.Bloc bloc,bool additive)
        {
            foreach(Line line in m_lines)
            {
                line.SelectLines(lines, bloc, additive);
            }
        }

        public void SelectAllLines()
        {
            foreach(d.Bloc bloc in TrialMatrixData.Blocs)
            {
                int[] linesSelected = new int[bloc.Lines.Length];
                for (int i = 0; i < linesSelected.Length; i++)
                {
                    linesSelected[i] = i;
                }
                SelectLines(linesSelected, bloc.PBloc, true);
            }
        }
        #endregion

        #region Private Methods
        void AddLine(d.Bloc[] blocsInLine,int max,Texture2D colorMap,Vector2 limits)
        {
            GameObject l_lineGameObject = Instantiate(m_linePrefab);
            l_lineGameObject.transform.SetParent(transform.GetChild(0).GetChild(0));
            Line l_line = l_lineGameObject.GetComponent<Line>();
            l_line.Set(blocsInLine,max, colorMap,limits);

            //l_line.SelectLinesEvent.AddListener((lines, bloc) => SelectLinesEventHandler(lines, bloc));
            m_lines.Add(l_line);
        }

        void SetLegends(Vector2 valueslimits,Vector2[] timeLimitsByColumn)
        {
            m_value.Set(m_colorMap, valueslimits.x, valueslimits.y,5);
            m_timeBlocs.Set(timeLimitsByColumn);
        }

        void OnRectTransformDimensionsChange()
        {
            CalculateSize();
        }

        void CalculateSize()
        {
            RectTransform l_rect = transform as RectTransform;
            float height = (l_rect.rect.width-75) * (m_lines.Count * m_ratio) / (m_maxBlocsByLine);
            m_layout.preferredHeight = height+95;
        }
        #endregion
    }
}