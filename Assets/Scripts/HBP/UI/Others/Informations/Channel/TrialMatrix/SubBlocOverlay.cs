using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using data = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{  
    public class SubBlocOverlay : MonoBehaviour
    {
        #region Properties
        public SubBloc SubBloc;
        [SerializeField] Text m_TrialText;
        [SerializeField] Text m_ValueText;
        [SerializeField] Text m_LatencyText;

        RectTransform m_BlocRectTransform;
        bool m_Initialized;
        #endregion

        #region Private Methods
        void Update()
        {
            //Display();
        }
        void Awake()
        {
            m_BlocRectTransform = SubBloc.transform.GetComponent<RectTransform>();
        }
        void Display()
        {
            data.SubBloc subBloc = SubBloc.Data;
            data.SubTrial[] subTrials = subBloc.SubTrials;
            Vector2 ratio = m_BlocRectTransform.GetRatioPosition(Input.mousePosition);

            int trial = Mathf.FloorToInt((1-ratio.y) * subTrials.Length);
            data.SubTrial subTrial = subTrials[trial];
            float[] values = subTrial.Data.Values;
            int sample = Mathf.Clamp(Mathf.FloorToInt(ratio.x * values.Length), 0, values.Length - 1);
            float value = values[sample];
            float latency = subBloc.Window.Start + ratio.x * subBloc.Window.Lenght;

            m_TrialText.text = (trial + 1) + "/" + subTrials.Length;
            m_ValueText.text = value.ToString("N2") + " " + subTrial.Data.Unit;
            m_LatencyText.text = latency.ToString("N2") + " ms";
        }
        #endregion
    }
}