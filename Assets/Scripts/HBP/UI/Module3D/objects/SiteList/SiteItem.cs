using HBP.Module3D;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteItem : Item<Site>
    {
        #region Properties
        [SerializeField]
        private Text m_Label;
        [SerializeField]
        private Image m_Included;
        [SerializeField]
        private Image m_NotBlacklisted;
        [SerializeField]
        private Image m_InAROI;
        [SerializeField]
        private Image m_Marked;
        [SerializeField]
        private Image m_Highlighted;

        public override Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_Label.text = value.Information.Patient.Name + "_" + value.Information.Name;
                m_Label.color = !value.State.IsMasked ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
                m_Included.color = !value.State.IsExcluded ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
                m_NotBlacklisted.color = !value.State.IsBlackListed ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
                m_InAROI.color = !value.State.IsOutOfROI ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
                m_Marked.color = value.State.IsMarked ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
                m_Highlighted.color = value.State.IsHighlighted ? ApplicationState.GeneralSettings.Theme.General.OK : ApplicationState.GeneralSettings.Theme.General.Error;
            }
        }
        #endregion
    }
}