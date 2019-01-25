using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    [ExecuteInEditMode]
    public class Axe : MonoBehaviour
    {
        #region Properties
        public enum DirectionEnum { LeftToRight, RightToLeft, BottomToTop, TopToBottom}
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
                if (m_DisplayRange != value)
                {
                    m_DisplayRange = value;
                    float ratio, step, startIndex, v, position; int numberOfMajorTickMarksNeeded;
                    CalculateAxeValue(value, out ratio, out step, out numberOfMajorTickMarksNeeded, out startIndex);

                    for (int i = 0; i < m_UsedTickMarks.Length; i++)
                    {
                        if (i < numberOfMajorTickMarksNeeded)
                        {
                            v = (startIndex + i) * step;
                            position = (v - value.x) * ratio;
                            MajorTickMark tickMark = m_UsedTickMarks[i];
                            tickMark.Label = v.ToString();
                            tickMark.Position = position;
                            tickMark.Direction = Direction;
                            tickMark.Color = m_Color;
                        }
                        else
                        {
                            m_UsedTickMarks[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        [SerializeField] RectTransform m_TickMarkContainer;
        [SerializeField] MajorTickMark[] m_TickMarkPool;
        [SerializeField] MajorTickMark[] m_UsedTickMarks;
        [SerializeField] MajorTickMark m_IndependentTickMark;
        #endregion

        #region Private Methods
        void CalculateAxeValue(Vector2 limits, out float ratio, out float step, out int numberOfTickMarksNeeded, out float startIndex)
        {
            // Calculate the lenght of the axe.
            float lenght = limits.y - limits.x;

            // Calculate the normalized range(1-10) of the axe.
            float normalizedLenght = lenght;
            float coef = 1f;
            if (normalizedLenght > 0)
            {
                while (normalizedLenght >= 10.0f)
                {
                    coef *= 10.0f;
                    normalizedLenght /= 10.0f;
                    break;
                }
                while (normalizedLenght < 1.0f)
                {
                    coef /= 10.0f;
                    normalizedLenght *= 10.0f;
                    break;
                }
                // Calculate the normalizedStep then the Step.
                float normalizedStep = normalizedLenght / m_TickMarkPool.Length;
                normalizedStep = (Mathf.Ceil(normalizedStep * 2.0f)) / 2.0f;
                step = normalizedStep * coef;

                // Calculate the firstScalePoint of the axe
                if (limits.x < 0.0f)
                {
                    startIndex = Mathf.CeilToInt(limits.x / step);
                }
                else
                {
                    startIndex = Mathf.CeilToInt(limits.y / step);
                }

                // Calculate the number of ScalePoint in the axe
                numberOfTickMarksNeeded = 0;
                while ((numberOfTickMarksNeeded + startIndex) * step <= limits.y)
                {
                    numberOfTickMarksNeeded += 1;
                }

                float axeSize = 0;
                switch (Direction)
                {
                    case DirectionEnum.LeftToRight:
                    case DirectionEnum.RightToLeft:
                        axeSize = m_TickMarkContainer.rect.width;
                        break;
                    case DirectionEnum.TopToBottom:
                    case DirectionEnum.BottomToTop:
                        axeSize = m_TickMarkContainer.rect.height;
                        break;
                }
                // Find the value of the scalesPoints
                ratio = axeSize / lenght;
            }
            else
            {
                ratio = 0;
                step = 0;
                numberOfTickMarksNeeded = 0;
                startIndex = 0;
            }
        }
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
        void OnValidate()
        {
            SetLabel();
            SetUnit();
        }
        #endregion
    }
}