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
  
            // TODO
            //float latency = dataBloc.ProtocolBloc.Window.Start + ratio.x * (dataBloc.ProtocolBloc.Window.End - dataBloc.ProtocolBloc.Window.Start);

            m_ValueText.text = value.ToString("N2") + " mV";
            // TODO
            //m_LatencyText.text = latency.ToString("N2") + " ms";
        }
        #endregion
    }
}