using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Module3D
{
    public class AdvancedSiteConditionItem : SelectableItem<string>
    {
        #region Properties
        [SerializeField] Text m_ConditionText;
        [SerializeField] Button m_RemoveButton;

        /// <summary>
        /// Site to display.
        /// </summary>
        public override string Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                UpdateFields();
            }
        }
        /// <summary>
        /// True if a update if required, False otherwise.
        /// </summary>
        private bool m_UpdateRequired;
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_RemoveButton.onClick.AddListener(() =>
            {
                AdvancedSiteConditionStrings.RemoveCondition(Object);
            });
        }
        /// <summary>
        /// Update all fields.
        /// </summary>
        private void UpdateFields()
        {
            m_ConditionText.text = Object;
        }
        #endregion
    }
}