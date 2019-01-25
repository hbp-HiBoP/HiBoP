using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class SimplifiedAxe : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject GraduationPrefab;
        public RectTransform GraduationRectTransform;

        public Graphic[] Graphics;
        public RectTransform VisualRectTransform;
        public Axe.DirectionEnum Direction;

        MajorTickMark[] m_Graduations = new MajorTickMark[10];
        Color m_Color;
        #endregion

        #region Public Methods
        public void SetColor(Color color)
        {
            m_Color = color;
            foreach (MajorTickMark majorTickMark in m_Graduations) majorTickMark.Color = color;
            foreach (Graphic graphic in Graphics) graphic.color = color;
        }
        public void SetLimits(Vector2 limits)
        {
            //// Calculate the value of the axe.
            //float ratio, step, startIndex, value, position; int numberOfMajorTickMarksNeeded;
            //CalculateAxeValue(limits.y, limits.x, out ratio, out step, out numberOfMajorTickMarksNeeded, out startIndex);

            ////// Add the graduations
            //for (int i = 0; i < m_Graduations.Length; i++)
            //{
            //    if (i < numberOfMajorTickMarksNeeded)
            //    {
            //        value = (startIndex + i) * step;
            //        position = (value - limits.x) * ratio;
            //        SetGraduations(m_Graduations[i], value.ToString(), position, Side, m_Color);
            //    }
            //    else
            //    {
            //        m_Graduations[i].gameObject.SetActive(false);
            //    }
            //}
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            // Instantiate tick marks.
            InstantiateGraduations();
        }
        void SetGraduations(MajorTickMark majorTickMark, string label, float position, Axe.DirectionEnum direction, Color color)
        {
            majorTickMark.Label = label;
            majorTickMark.Position = position;
            majorTickMark.Direction = direction;
            majorTickMark.Color = color;
        }
        void InstantiateGraduations()
        {
            for (int i = 0; i < m_Graduations.Length; i++)
            {
                GameObject tickMarkGameObject = Instantiate(GraduationPrefab, GraduationRectTransform);
                MajorTickMark majorTickMark = tickMarkGameObject.GetComponent<MajorTickMark>();
                m_Graduations[i] = majorTickMark;
            }
        }
        void CalculateAxeValue(float max, float min, out float ratio, out float step, out int numberOfTrickMarkNeeded, out float startIndex)
        {
            // Calculate the range of the axe.
            float lenght = max - min;

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
                float normalizedStep = normalizedLenght / m_Graduations.Length;
                normalizedStep = (Mathf.Ceil(normalizedStep * 2.0f)) / 2.0f;
                step = normalizedStep * coef;

                // Calculate the firstScalePoint of the axe
                if (min < 0.0f)
                {
                    startIndex = Mathf.CeilToInt(min / step);
                }
                else
                {
                    startIndex = Mathf.CeilToInt(min / step);
                }

                // Calculate the number of ScalePoint in the axe
                numberOfTrickMarkNeeded = 0;
                while ((numberOfTrickMarkNeeded + startIndex) * step <= max)
                {
                    numberOfTrickMarkNeeded += 1;
                }

                float axeSize = 0;
                switch (Direction)
                {
                    case Axe.DirectionEnum.LeftToRight:
                    case Axe.DirectionEnum.RightToLeft:
                        axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.width;
                        break;
                    case Axe.DirectionEnum.TopToBottom:
                    case Axe.DirectionEnum.BottomToTop:
                        axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.height;
                        break;
                }
                // Find the value of the scalesPoints
                ratio = axeSize / lenght;
            }
            else
            {
                ratio = 0;
                step = 0;
                numberOfTrickMarkNeeded = 0;
                startIndex = 0;
            }
        }
        #endregion
    }
}