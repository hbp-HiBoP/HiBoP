using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class OpenInteractiveViewer : Tool
    {
        #region Properties
        private string m_BaseURL = @"https://kg.humanbrainproject.org/viewer/?templateSelected=MNI+152+ICBM+2009c+Nonlinear+Asymmetric&parcellationSelected=Fibre+Bundle+Atlas+-+Long+Bundle";
        [SerializeField] private Button m_Button;
        #endregion

        #region Private Methods
        private void Open()
        {
            // TODO : replace by correct values
            string url = string.Format("{0}&navigation={1}_{2}_{3}_{4}__{5}_{6}_{7}_{8}__{9}__{10}_{11}_{12}__{13}",
                m_BaseURL,
                0, 0, 0, 1,
                "-0.3", "0.7", "-0.5", "0.2",
                3000000,
                0, 0, 0,
                300000);
            Application.OpenURL(url);
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Open();
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isSiteSelected = SelectedColumn.SelectedSite != null;

            m_Button.interactable = isSiteSelected;
        }
        #endregion
    }
}