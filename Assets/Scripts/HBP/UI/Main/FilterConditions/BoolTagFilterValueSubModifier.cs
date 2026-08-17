using HBP.Core.Data;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class BoolTagFilterValueSubModifier : SubModifier<BoolTagFilterValue>
    {
        #region Properties

        [SerializeField] Toggle m_ValueToggle;

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_ValueToggle.onValueChanged.AddListener(value => Object.Value = value);
        }

        #endregion

        #region Protected Methods

        protected override void SetFields(BoolTagFilterValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ValueToggle.isOn = objectToDisplay.Value;
        }

        #endregion
    }
}
