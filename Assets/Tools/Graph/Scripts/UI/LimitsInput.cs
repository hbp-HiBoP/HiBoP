using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

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
        public Limits Limits
        {
            get
            {
                if(NumberExtension.TryParseFloat(MinAbscissaInputField.text, out float abscissaMin) &&
                   NumberExtension.TryParseFloat(MaxAbscissaInputField.text, out float abscissaMax) &&
                   NumberExtension.TryParseFloat(MinOrdinateInputField.text, out float ordinateMin) &&
                   NumberExtension.TryParseFloat(MaxOrdinateInputField.text, out float ordinateMax))
                {
                    return new Limits(abscissaMin, abscissaMax, ordinateMin, ordinateMax);
                }
                else
                {
                    Debug.LogError("Can't parse the limits");
                    return new Limits();
                }
            }
            set
            {
                CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
                MinAbscissaInputField.text = value.AbscissaMin.ToString(Format, cultureInfo);
                MaxAbscissaInputField.text = value.AbscissaMax.ToString(Format, cultureInfo);
                MinOrdinateInputField.text = value.OrdinateMin.ToString(Format, cultureInfo);
                MaxOrdinateInputField.text = value.OrdinateMax.ToString(Format, cultureInfo);
                OnChangeLimits.Invoke(value);
            }
        }
        public LimitsEvent OnChangeLimits;

        public string Format = "N2";
        public string CultureInfo = "en-US";
        #endregion
    }
}