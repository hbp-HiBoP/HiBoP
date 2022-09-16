using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class OffsetTreatmentSubModifier : SubModifier<Core.Data.OffsetTreatment>
    {
        #region Properties
        [SerializeField] InputField m_OffsetInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_OffsetInputField.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_OffsetInputField.onEndEdit.AddListener(OnChangeOffset);
        }
        public void OnChangeOffset(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float floatResult))
            {
                Object.Offset = floatResult;
            }
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.OffsetTreatment objectToDisplay)
        {
            m_OffsetInputField.text = objectToDisplay.Offset.ToString("0.##", CultureInfo.InvariantCulture);
        }
        #endregion
    }
}