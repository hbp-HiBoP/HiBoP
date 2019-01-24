using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class LimitsInput : MonoBehaviour
    {
        #region Properties
        public Text AbscissaText;
        public string Abscissa
        {
            get
            {
                return AbscissaText.text;
            }
            set
            {
                AbscissaText.text = value;
            }
        }

        public Text OrdinateText;
        public string Ordinate
        {
            get
            {
                return OrdinateText.text;
            }
            set
            {
                OrdinateText.text = value;
            }
        }

        public InputField MinAbscissaInputField;
        public InputField MaxAbscissaInputField;
        public InputField MinOrdinateInputField;
        public InputField MaxOrdinateInputField;

        public Vector2 OrdinateDisplayRange
        {
            get
            {
                Vector2 displayRange = new Vector2();
                if (NumberExtension.TryParseFloat(MinOrdinateInputField.text, out float min))
                {
                    displayRange.x = min;
                }
                else Debug.LogError("Can't parse ordinate min value");
                if (NumberExtension.TryParseFloat(MaxOrdinateInputField.text, out float max))
                {
                    displayRange.y = max;
                }
                else Debug.LogError("Can't parse ordinate max value");
                return displayRange;
            }
            set
            {
                CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
                MinOrdinateInputField.text = value.x.ToString(Format, cultureInfo);
                MaxOrdinateInputField.text = value.y.ToString(Format, cultureInfo);
                OnChangeAbscissaDisplayRange.Invoke(value);
            }
        }
        public Vector2Event OnChangeOrdinateDisplayRange;

        public Vector2 AbscissaDisplayRange
        {
            get
            {
                Vector2 displayRange = new Vector2();
                if (NumberExtension.TryParseFloat(MinAbscissaInputField.text, out float abscissaMin))
                {
                    displayRange.x = abscissaMin;
                }
                else Debug.LogError("Can't parse abscissa min value");
                if (NumberExtension.TryParseFloat(MaxAbscissaInputField.text, out float abscissaMax))
                {
                    displayRange.y = abscissaMax;
                }
                else Debug.LogError("Can't parse abscissa max value");
                return displayRange;
            }
            set
            {
                CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
                MinAbscissaInputField.text = value.x.ToString(Format, cultureInfo);
                MaxAbscissaInputField.text = value.y.ToString(Format, cultureInfo);
                OnChangeAbscissaDisplayRange.Invoke(value);
            }
        }
        public Vector2Event OnChangeAbscissaDisplayRange;

        public string Format = "N2";
        public string CultureInfo = "en-US";
        #endregion
    }
}