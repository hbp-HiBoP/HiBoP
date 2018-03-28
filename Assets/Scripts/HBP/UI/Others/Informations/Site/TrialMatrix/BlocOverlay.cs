using UnityEngine;
using UnityEngine.UI;
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
        RectTransform m_RectTransform;
        bool m_Initialized;
        #endregion

        #region Private Methods
        void Awake()
        {
            if (!m_Initialized) Initialize();
        }
        void Update()
        {
            UpdatePosition();
            UpdateValues();
        }
        void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_BlocRectTransform = Bloc.transform.GetComponent<RectTransform>();
            m_Initialized = true;
        }
        void UpdatePosition()
        {
            transform.position = Input.mousePosition;
            Vector3[] corners = new Vector3[4];
            Vector3[] parentCorners = new Vector3[4];
            m_RectTransform.GetWorldCorners(corners);
            m_BlocRectTransform.GetWorldCorners(parentCorners);

            float botPosition = m_RectTransform.position.y - m_RectTransform.rect.height;
            float rightPosition = m_RectTransform.position.x + m_RectTransform.rect.width;

            if (botPosition > parentCorners[0].y && rightPosition < parentCorners[2].x)
            {
                m_RectTransform.pivot = new Vector2(0, 1);
            }
            else
            {
                if (corners[2].x > parentCorners[2].x)
                {
                    m_RectTransform.pivot = new Vector2(1, m_RectTransform.pivot.y);
                }
                if (corners[0].y < parentCorners[0].y)
                {
                    m_RectTransform.pivot = new Vector2(m_RectTransform.pivot.x, 0);
                }
            }
        }
        void UpdateValues()
        {
            data.Bloc dataBloc = Bloc.Data;
            data.Line[] trials = dataBloc.Trials;

            Vector2 ratio = GetRatio();

            int trial = Mathf.FloorToInt(ratio.y * trials.Length);
            int sample = Mathf.FloorToInt(ratio.x * dataBloc.Trials[trial].NormalizedValues.Length);
            float value = trials[trial].NormalizedValues[sample];
            float latency = dataBloc.ProtocolBloc.Window.Start + ratio.x * (dataBloc.ProtocolBloc.Window.End - dataBloc.ProtocolBloc.Window.Start);

            m_ValueText.text = value.ToString() + " mV";
            m_TrialText.text = trial.ToString();
            m_LatencyText.text = latency.ToString() + "ms (" + sample.ToString() +")";
        }
        Vector2 GetRatio()
        {
            Vector2 localPosition = Input.mousePosition - m_BlocRectTransform.position - (Vector3) m_BlocRectTransform.rect.min;
            Vector2 ratio = new Vector2(localPosition.x / m_BlocRectTransform.rect.width, localPosition.y / m_BlocRectTransform.rect.height);
            Vector2 clampedRatio = new Vector2(Mathf.Clamp01(ratio.x), Mathf.Clamp01(ratio.y));
            return clampedRatio;
        }
        #endregion
    }
}