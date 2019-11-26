using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
            Vector3 sitePosition = ApplicationState.Module3D.SelectedColumn.SelectedSite.transform.localPosition;
            Quaternion quaternion = new Quaternion(-0.3f, 0.7f, -0.5f, 0.2f);
            View3D view = ApplicationState.Module3D.SelectedView;
            if (view)
            {
                quaternion = view.LocalCameraRotation * Quaternion.Euler(0, 180, 0);
            }
            string url = string.Format("{0}&navigation={1}_{2}_{3}_{4}__{5}_{6}_{7}_{8}__{9}__{10}_{11}_{12}__{13}",
                m_BaseURL,
                0, 0, 0, 1,
                quaternion.x.ToString("0.00", new CultureInfo("en-US")), (-quaternion.y).ToString("0.00", new CultureInfo("en-US")), (-quaternion.z).ToString("0.00", new CultureInfo("en-US")), quaternion.w.ToString("0.00", new CultureInfo("en-US")),
                3000000,
                (-sitePosition.x * 1000000).ToString("0.00", new CultureInfo("en-US")), (sitePosition.y * 1000000).ToString("0.00", new CultureInfo("en-US")), (sitePosition.z * 1000000).ToString("0.00", new CultureInfo("en-US")),
                150000);
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