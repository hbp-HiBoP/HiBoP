using HBP.Display.Module3D;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class OpenInteractiveViewer : Tool
    {
        #region Properties
        /// <summary>
        /// Base URL of the interactive viewer
        /// </summary>
        private string m_BaseURL = @"https://kg.humanbrainproject.org/viewer/?templateSelected=MNI+152+ICBM+2009c+Nonlinear+Asymmetric&parcellationSelected=Fibre+Bundle+Atlas+-+Long+Bundle&navigation={0}_{1}_{2}_{3}__{4}_{5}_{6}_{7}__{8}__{9}_{10}_{11}__{12}";
        /// <summary>
        /// Open the HBP interactive viewer
        /// </summary>
        [SerializeField] private Button m_Button;
        #endregion

        #region Private Methods
        /// <summary>
        /// Open the interactive viewer
        /// </summary>
        private void Open()
        {
            // TODO : replace by correct values
            Vector3 sitePosition = HBP3DModule.SelectedColumn.SelectedSite.transform.localPosition;
            Quaternion quaternion = new Quaternion(-0.3f, 0.7f, -0.5f, 0.2f);
            View3D view = HBP3DModule.SelectedView;
            if (view)
            {
                quaternion = view.LocalCameraRotation * Quaternion.Euler(0, 180, 0);
            }
            string url = string.Format(m_BaseURL,
                0, 0, 0, 1,
                quaternion.x.ToString("0.00", new CultureInfo("en-US")), (-quaternion.y).ToString("0.00", new CultureInfo("en-US")), (-quaternion.z).ToString("0.00", new CultureInfo("en-US")), quaternion.w.ToString("0.00", new CultureInfo("en-US")),
                3000000,
                (-sitePosition.x * 1000000).ToString("0.00", new CultureInfo("en-US")), (sitePosition.y * 1000000).ToString("0.00", new CultureInfo("en-US")), (sitePosition.z * 1000000).ToString("0.00", new CultureInfo("en-US")),
                150000);
            Application.OpenURL(url);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Open();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isSiteSelected = SelectedColumn.SelectedSite != null;

            m_Button.interactable = isSiteSelected;
        }
        #endregion
    }
}