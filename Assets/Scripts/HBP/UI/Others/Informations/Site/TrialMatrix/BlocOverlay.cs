using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using data = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{  
    public class BlocOverlay : MonoBehaviour
    {
        #region Properties
        public SubBloc Bloc;
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
            data.SubTrial[] trials = dataBloc.SubBlocs;

            Vector2 ratio = m_BlocRectTransform.GetRatioPosition(Input.mousePosition);

            int trial = Mathf.Clamp(Mathf.FloorToInt(ratio.y * trials.Length),0,trials.Length-1);
            int sample = Mathf.Clamp(Mathf.FloorToInt(ratio.x * dataBloc.SubBlocs[trial].NormalizedValues.Length),0,trials[trial].NormalizedValues.Length-1);
            float value = trials[trial].NormalizedValues[sample];
            // TODO
            //float latency = dataBloc.ProtocolBloc.Window.Start + ratio.x * (dataBloc.ProtocolBloc.Window.End - dataBloc.ProtocolBloc.Window.Start);

            m_ValueText.text = value.ToString("N2") + " mV";
            // TODO
            //m_LatencyText.text = latency.ToString("N2") + " ms";
        }
        #endregion
    }
}