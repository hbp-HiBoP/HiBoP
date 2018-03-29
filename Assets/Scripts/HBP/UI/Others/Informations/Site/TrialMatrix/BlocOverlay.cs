using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using data = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{  
    public class BlocOverlay : MonoBehaviour
    {
        #region Properties
        public Bloc Bloc;
        [SerializeField] Text m_TrialText;
        [SerializeField] Text m_ValueText;
        [SerializeField] Text m_LatencyText;

        RectTransform m_BlocRectTransform;
        bool m_Initialized;
        #endregion

        #region Private Methods
        void Update()
        {
            if (!m_Initialized) Initialize();
            Display();
        }
        void Initialize()
        {
            m_BlocRectTransform = Bloc.transform.GetComponent<RectTransform>();
            m_Initialized = true;
        }
        void Display()
        {
            data.Bloc dataBloc = Bloc.Data;
            data.Line[] trials = dataBloc.Trials;

            Vector2 ratio = m_BlocRectTransform.GetRatioPosition(Input.mousePosition);

            int trial = Mathf.FloorToInt(ratio.y * trials.Length);
            int sample = Mathf.FloorToInt(ratio.x * dataBloc.Trials[trial].NormalizedValues.Length);
            float value = trials[trial].NormalizedValues[sample];
            float latency = dataBloc.ProtocolBloc.Window.Start + ratio.x * (dataBloc.ProtocolBloc.Window.End - dataBloc.ProtocolBloc.Window.Start);

            m_ValueText.text = value.ToString() + " mV";
            m_TrialText.text = trial.ToString();
            m_LatencyText.text = latency.ToString() + " ms (" + sample.ToString() + ")";
        }
        #endregion
    }
}