using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;
using Tools.CSharp;
using System.Globalization;

namespace HBP.UI.Anatomy
{
    public class CoordinateModifier : ObjectModifier<Coordinate>
    {
        #region Properties
        [SerializeField] InputField m_ReferenceSystemInputField;
        [SerializeField] InputField m_XInputField;
        [SerializeField] InputField m_YInputField;
        [SerializeField] InputField m_ZInputField;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_ReferenceSystemInputField.interactable = value;
                m_XInputField.interactable = value;
                m_YInputField.interactable = value;
                m_ZInputField.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(Coordinate objectToDisplay)
        {
            m_ReferenceSystemInputField.text = objectToDisplay.ReferenceSystem;
            m_XInputField.text = objectToDisplay.Value.x.ToString(CultureInfo.InvariantCulture);
            m_YInputField.text = objectToDisplay.Value.y.ToString(CultureInfo.InvariantCulture);
            m_ZInputField.text = objectToDisplay.Value.z.ToString(CultureInfo.InvariantCulture);
        }
        protected override void Initialize()
        {
            base.Initialize();

            m_ReferenceSystemInputField.onValueChanged.AddListener((referenceSystem) => ItemTemp.ReferenceSystem = referenceSystem);

            m_XInputField.onValueChanged.AddListener(OnChangeX);
            m_YInputField.onValueChanged.AddListener(OnChangeY);
            m_ZInputField.onValueChanged.AddListener(OnChangeZ);
        }
        void OnChangeX(string value)
        {
            if(NumberExtension.TryParseFloat(value, out float x))
            {
                ItemTemp.Value = new SerializableVector3(x, ItemTemp.Value.y, ItemTemp.Value.z);
            }
        }
        void OnChangeY(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float y))
            {
                ItemTemp.Value = new SerializableVector3(ItemTemp.Value.x, y, ItemTemp.Value.z);
            }
        }
        void OnChangeZ(string value)
        {
            if (NumberExtension.TryParseFloat(value, out float z))
            {
                ItemTemp.Value = new SerializableVector3(ItemTemp.Value.x, ItemTemp.Value.y,z);
            }
        }
        #endregion
    }
}