using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Legend : MonoBehaviour
    {
        #region Properties
        Image image;
        Text label;
        #endregion

        #region Public Methods
        public void Set(CurveData curve)
        {
            image.color = curve.Color;
            label.color = curve.Color;
            label.text = curve.Label;
        }
        #endregion

        #region private Methods
        void Awake()
        {
            image = GetComponentInChildren<Image>();
            label = GetComponentInChildren<Text>();
        }
        #endregion
    }
}

