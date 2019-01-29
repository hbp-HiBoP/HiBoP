using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    [ExecuteInEditMode]
    public class Axe : MonoBehaviour
    {
        #region Properties
        public enum DirectionEnum { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        [SerializeField] DirectionEnum m_Direction;
        public DirectionEnum Direction
        {
            get
            {
                return m_Direction;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Direction, value))
                {
                    SetType();
                }
            }
        }

        [SerializeField] Color m_Color;
        public Color Color
        {
            get
            {
                return m_Color;
            }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_Color, value))
                {
                    SetColor();
                }
            }
        }

        [SerializeField] Text m_UnitText;
        [SerializeField] string m_Unit;
        public string Unit
        {
            get
            {
                return m_Unit;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Unit, value))
                {
                    SetUnit();
                }
            }
        }

        [SerializeField] Text m_LabelText;
        [SerializeField] string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Label, value))
                {
                    SetLabel();
                }
            }
        }

        [SerializeField] Vector2 m_DisplayRange;
        public Vector2 DisplayRange
        {
            get
            {
                return m_DisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_DisplayRange, value))
                {
                    //SetDisplayRange();
                }
            }
        }

        [SerializeField] RectTransform m_TickMarkContainer;
        [SerializeField] List<MajorTickMark> m_TickMarkPool;
        [SerializeField] List<MajorTickMark> m_UsedTickMarks;
        [SerializeField] MajorTickMark m_IndependentTickMark;
        #endregion

        #region Private Setter
        void SetColor()
        {
            if (m_Color != null)
            {
                foreach (var tickMark in m_UsedTickMarks) tickMark.Color = m_Color;
                foreach (var tickMark in m_TickMarkPool) tickMark.Color = m_Color;
                if (m_IndependentTickMark != null) m_IndependentTickMark.Color = m_Color;
            }
        }
        void SetLabel()
        {
            if (m_LabelText != null && m_Label != null) m_LabelText.text = m_Label;
        }
        void SetUnit()
        {
            if (m_UnitText != null && m_Unit != null) m_UnitText.text = string.Format("({0})", m_Unit);
        }
        void SetType()
        {

        }
        void SetDisplayRange()
        {
            m_UsedTickMarks.ForEach(tickMark => { m_TickMarkPool.Add(tickMark); tickMark.gameObject.SetActive(false); });
            m_UsedTickMarks.Clear();

            double range = m_DisplayRange.y - m_DisplayRange.x;
            if(m_TickMarkPool.Count > 1 && range > 0)
            {
                double normalizedStep = range / (m_TickMarkPool.Count - 1);
                double coef = 1;

                if (normalizedStep < 1)
                {
                    coef /= 10;
                    normalizedStep *= 10;
                }
                while (normalizedStep > 10)
                {
                    double tempResult;
                    tempResult = normalizedStep / 2;
                    if (tempResult >= 1 && tempResult <= 10)
                    {
                        coef *= 2;
                        normalizedStep = tempResult;
                        break;
                    }

                    tempResult = normalizedStep / 5;
                    if (tempResult >= 1 && tempResult <= 10)
                    {
                        normalizedStep = tempResult;
                        coef *= 5;
                        break;
                    }

                    tempResult = normalizedStep / 10;
                    if (normalizedStep >= 1 && normalizedStep <= 10)
                    {
                        normalizedStep = tempResult;
                        coef *= 10;
                        break;
                    }

                    coef *= 10;
                    normalizedStep /= 10;
                }
                if (normalizedStep > 1 && normalizedStep <= 5)
                {
                    normalizedStep = 5;
                }
                else if (normalizedStep > 5 && normalizedStep <= 10)
                {
                    normalizedStep = 10;
                }
                double step = normalizedStep * coef;

                List<double> tickMarks = new List<double>();
                int division = Mathf.FloorToInt((float)(m_DisplayRange.x / step));
                double rest = m_DisplayRange.x % step;
                if (rest != 0) division++;
                double value = division * step;
                while (value <= m_DisplayRange.y)
                {
                    tickMarks.Add(value);
                    value += step;
                }

                for (int i = 0; i < tickMarks.Count; i++)
                {
                    MajorTickMark tickMark = m_TickMarkPool.First();

                    tickMark.Label = tickMarks[i].ToString();
                    tickMark.Position = ((float)tickMarks[i] - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
                    tickMark.Direction = m_Direction;
                    tickMark.Color = m_Color;
                    tickMark.gameObject.SetActive(true);
                    m_TickMarkPool.Remove(tickMark);
                    m_UsedTickMarks.Add(tickMark);
                }
            }
  
        }
        void OnValidate()
        {
            SetLabel();
            SetUnit();
            SetDisplayRange();
        }
        #endregion
    }
}