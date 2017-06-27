using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SelectedSite : Tool
    {
        #region Properties
        [SerializeField]
        private Text m_Text;

        public GenericEvent<Site> OnChangeValue = new GenericEvent<Site>();
        #endregion

        #region Public Methods
        public override void AddListeners()
        {
            ApplicationState.Module3D.OnSelectSite.AddListener((site) =>
            {
                if (site)
                {
                    m_Text.text = site.Information.FullName;
                }
                else
                {
                    DefaultState();
                }
            });
        }
        public override void DefaultState()
        {
            m_Text.text = "No site selected";
        }
        public override void UpdateInteractable()
        {

        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                Site site = ApplicationState.Module3D.SelectedColumn.SelectedSite;
                m_Text.text = site ? site.Information.FullName : "No site selected";
            }
        }
        #endregion
    }
}