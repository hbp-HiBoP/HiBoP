using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

namespace Tools.Unity.Graph
{
    public class Axe : MonoBehaviour
    {
        #region Attributs
        [SerializeField]
        GameObject MajorTickMarkPrefab;

        Text labelText;
        RectTransform tickMarkRectTransform;
        RectTransform visualRectTransform;
        MajorTickMark[] majorTickMarks = new MajorTickMark[10];

        Color color;
        public enum SideEnum { absciss, ordinate };
        public SideEnum Side;
        #endregion

        #region Public Methods
        public void SetLabel(string label)
        {
            labelText.text = label;
        }
        public void SetColor(Color color)
        {
            // Save color.
            this.color = color;

            // Set color of the tickmarks.
            foreach (MajorTickMark majorTickMark in majorTickMarks) majorTickMark.SetColor(color);

            //Set color of the axe visual.
            visualRectTransform.GetComponent<Image>().color = color;
            visualRectTransform.GetChild(0).GetComponent<Image>().color = color;

            //Set color of the label.
            labelText.color = color;
        }
        public void SetLimits(Vector2 limits)
        {
            Profiler.BeginSample("Calculate axe value");
            // Calculate the value of the axe.
            float ratio,step,startIndex,value,position; int numberOfMajorTickMarksNeeded;
            CalculateAxeValue(limits.y, limits.x, out ratio, out step, out numberOfMajorTickMarksNeeded, out startIndex);
            Profiler.EndSample();

            //// Add the graduations
            Profiler.BeginSample("Set graduations");
            for (int i = 0; i < majorTickMarks.Length; i++)
            {
                if(i < numberOfMajorTickMarksNeeded)
                {
                    value = (startIndex + i) * step;
                    position = (value - limits.x) * ratio;
                    SetMajorTickMarks(majorTickMarks[i], value.ToString(), position, Side, color);
                }
                else
                {
                    majorTickMarks[i].gameObject.SetActive(false);
                }
            }
            Profiler.EndSample();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            labelText = transform.FindChild("Label").GetComponentInChildren<Text>();
            tickMarkRectTransform = transform.FindChild("Axe").FindChild("Graduations") as RectTransform;
            visualRectTransform = transform.FindChild("Axe").FindChild("Visual") as RectTransform;

            // Instante tick marks.
            InstantiateMajorTickMarks();
        }
        void SetMajorTickMarks(MajorTickMark majorTickMark, string label, float position, SideEnum side, Color color)
        {
            majorTickMark.Set(label, position, side, color);
        }
        void InstantiateMajorTickMarks()
        {
            for (int i = 0; i < majorTickMarks.Length; i++)
            {
                GameObject tickMarkGameObject = Instantiate(MajorTickMarkPrefab, tickMarkRectTransform);
                MajorTickMark majorTickMark = tickMarkGameObject.GetComponent<MajorTickMark>();
                majorTickMarks[i] = majorTickMark;
            }
        }
        void CalculateAxeValue(float max, float min, out float ratio, out float step, out int numberOfTrickMarkNeeded, out float startIndex)
        {
            // Calculate the range of the axe.
            float lenght = max - min;         

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
                float normalizedStep = normalizedLenght / majorTickMarks.Length;
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
                switch (Side)
                {
                    case SideEnum.absciss: axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.width; break;
                    case SideEnum.ordinate: axeSize = transform.GetChild(0).GetComponent<RectTransform>().rect.height; break;
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