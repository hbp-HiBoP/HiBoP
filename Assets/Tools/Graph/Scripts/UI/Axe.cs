using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class Axe : MonoBehaviour
    {
        #region Properties
        public enum SideEnum { abscissa, ordinate };
        public SideEnum Side;

        Color m_Color;
        public Color Color
        {
            get
            {
                return m_Color;
            }
            set
            {
                if(m_Color != value)
                {
                    m_Color = value;
                    foreach (MajorTickMark majorTickMark in m_TickMarks) majorTickMark.SetColor(value);
                    OnChangeColor.Invoke(value);
                }
            }
        }
        public ColorEvent OnChangeColor;

        string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if(m_Title != value)
                {
                    m_Title = value;
                    OnChangeTitle.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeTitle;

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                if(m_Limits != value)
                {
                    m_Limits = value;
                    float ratio, step, startIndex, v, position; int numberOfMajorTickMarksNeeded;
                    CalculateAxeValue(value, out ratio, out step, out numberOfMajorTickMarksNeeded, out startIndex);

                    for (int i = 0; i < m_TickMarks.Length; i++)
                    {
                        if (i < numberOfMajorTickMarksNeeded)
                        {
                            v = (startIndex + i) * step;
                            position = (v - value.x) * ratio;
                            m_TickMarks[i].Set(v.ToString(), position, Side, m_Color);
                        }
                        else
                        {
                            m_TickMarks[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        [SerializeField] GameObject m_TickMarkPrefab;
        [SerializeField] RectTransform m_TickMarkContainer;

        const int MAX_NUMBER_OF_TICK_MARK = 10;
        MajorTickMark[] m_TickMarks = new MajorTickMark[MAX_NUMBER_OF_TICK_MARK];
        #endregion

        #region Private Methods
        void Awake()
        {
            CreatePool();
        }
        void CreatePool()
        {
            for (int i = 0; i < m_TickMarks.Length; i++)
            {
                GameObject tickMarkGameObject = Instantiate(m_TickMarkPrefab, m_TickMarkContainer);
                MajorTickMark majorTickMark = tickMarkGameObject.GetComponent<MajorTickMark>();
                m_TickMarks[i] = majorTickMark;
            }
        }
        void CalculateAxeValue(Vector2 limits, out float ratio, out float step, out int numberOfTickMarksNeeded, out float startIndex)
        {
            // Calculate the lenght of the axe.
            float lenght = limits.y - limits.x;         

            // Calculate the normalized range(1-10) of the axe.
            float normalizedLenght = lenght;
            float coef = 1f;
            if(normalizedLenght > 0)
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
                float normalizedStep = normalizedLenght / m_TickMarks.Length;
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
                switch (Side)
                {
                    case SideEnum.abscissa: axeSize = m_TickMarkContainer.rect.width; break;
                    case SideEnum.ordinate: axeSize = m_TickMarkContainer.rect.height; break;
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
    }
}