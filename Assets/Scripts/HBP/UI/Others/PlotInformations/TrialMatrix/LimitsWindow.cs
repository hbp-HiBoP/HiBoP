using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.TrialMatrix
{
    public class LimitsWindow : MonoBehaviour
    {
        #region Properties
        public OnUpdateLimitsEvent onUpdateLimits = new OnUpdateLimitsEvent();
        InputField minInputField, maxInputField;
        #endregion

        #region Public Methods
        public void Open(Vector2 limits)
        {
            gameObject.SetActive(true);
            minInputField.text = limits.x.ToString();
            maxInputField.text = limits.y.ToString();
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        public void UpdateWindow()
        {
            Vector2 limits = new Vector2(float.Parse(minInputField.text), float.Parse(maxInputField.text));
            onUpdateLimits.Invoke((limits));
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            minInputField = transform.FindChild("Min").FindChild("Value").GetComponent<InputField>();
            maxInputField = transform.FindChild("Max").FindChild("Value").GetComponent<InputField>();
        }
        #endregion
    }

    public class OnUpdateLimitsEvent : UnityEvent<Vector2> { }
}