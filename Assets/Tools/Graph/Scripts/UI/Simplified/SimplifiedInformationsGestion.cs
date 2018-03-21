using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class SimplifiedInformationsGestion : MonoBehaviour
    {
        #region Properties
        public Text TitleText;
        public string Title
        {
            get { return TitleText.text; }
            set { TitleText.text = value; }
        }
        public SimplifiedAxe Abscissa;
        public SimplifiedAxe Ordinate;
        Limits m_Limits;
        #endregion

        #region Public Methods
        public void SetLimits(Limits limits)
        {
            this.m_Limits = limits;
            Abscissa.SetLimits(limits.Abscissa);
            Ordinate.SetLimits(limits.Ordinate);
        }
        public void SetAbscissaLimits(Vector2 limits)
        {
            Abscissa.SetLimits(limits);
        }
        public void SetOrdinateLimits(Vector2 limits)
        {
            Ordinate.SetLimits(limits);
        }
        public void SetColor(Color color)
        {
            TitleText.color = color;
            Abscissa.SetColor(color);
            Ordinate.SetColor(color);
        }
        #endregion
    }
}