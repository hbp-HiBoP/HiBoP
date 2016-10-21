using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HBP.UI.TrialMatrix
{
    public class WindowGestion : MonoBehaviour
    {
        [SerializeField]
        InputField m_min;

        [SerializeField]
        InputField m_max;

        [SerializeField]
        TrialMatrix trialMatrix;

        public void UpdateWindow()
        {
            trialMatrix.UpdateLimites(new Vector2(float.Parse(m_min.text), float.Parse(m_max.text)));
        }

        void OnEnable()
        {
            Vector2 limits = trialMatrix.Limits;
            m_min.text = limits.x.ToString();
            m_max.text = limits.y.ToString();
        }
    }

}
