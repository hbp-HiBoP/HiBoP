using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class EnumTagValueSubModifier : SubModifier<Core.Data.EnumTagValue>
    {
        #region Properties
        [SerializeField] Dropdown m_ValueDropdown;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_ValueDropdown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_ValueDropdown.onValueChanged.AddListener(OnChangeValue);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.EnumTagValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_ValueDropdown.AddOptions(objectToDisplay.Tag.Values.ToList());
            m_ValueDropdown.value = objectToDisplay.Value;
        }
        void OnChangeValue(int value)
        {
            Object.Value = value;
        }
        #endregion
    }
}