using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class EnumTagFilterValueSubModifier : SubModifier<EnumTagFilterValue>
    {
        #region Properties

        [SerializeField] Dropdown m_ValueDropdown;
        public EnumTag Tag { get; set; }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_ValueDropdown.onValueChanged.AddListener(value => Object.Value = value);
        }

        #endregion

        #region Protected Methods

        protected override void SetFields(EnumTagFilterValue objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ValueDropdown.options = Tag.Values.Select(value => new Dropdown.OptionData(value.ToString())).ToList();
            m_ValueDropdown.SetValue(objectToDisplay.Value);
        }

        #endregion
    }
}
