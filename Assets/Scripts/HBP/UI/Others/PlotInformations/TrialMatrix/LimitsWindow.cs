using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.TrialMatrix
{
    public class LimitsWindow : MonoBehaviour
    {
        #region Properties
        [SerializeField] Button m_Auto;
        public GenericEvent<Vector2> OnUpdateLimits = new GenericEvent<Vector2>();
        public GenericEvent<bool> OnAutoLimits = new GenericEvent<bool>();
        InputField m_MinInputField, m_MaxInputField;
        #endregion

        #region Public Methods
        public void Open(Vector2 limits)
        {
            gameObject.SetActive(true);
            m_MinInputField.text = limits.x.ToString("n2");
            m_MaxInputField.text = limits.y.ToString("n2");
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        public void UpdateWindow()
        {
            Vector2 limits = new Vector2(float.Parse(m_MinInputField.text), float.Parse(m_MaxInputField.text));
            OnUpdateLimits.Invoke(limits);
            OnAutoLimits.Invoke(false);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_MinInputField = transform.Find("Min").Find("Value").GetComponent<InputField>();
            m_MaxInputField = transform.Find("Max").Find("Value").GetComponent<InputField>();
            m_Auto.onClick.AddListener(() =>
            {
                OnAutoLimits.Invoke(true);
            });
        }
        #endregion
    }
}