using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteInfoDisplayer : MonoBehaviour
    {
        #region Properties
        private SiteInformationDisplayMode m_CurrentMode = SiteInformationDisplayMode.IEEG;

        [SerializeField]
        private Text m_SiteName;

        [SerializeField]
        private Text m_PatientName;

        [SerializeField]
        private Text m_PatientDate;

        [SerializeField]
        private Text m_PatientPlace;

        [SerializeField]
        private Text m_IEEG;

        [SerializeField]
        private Text m_Height;

        [SerializeField]
        private Text m_Latency;

        [SerializeField]
        private Text m_MarsAtlas;

        [SerializeField]
        private Text m_Broadman;

        [SerializeField]
        private RectTransform m_Canvas;

        private RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_IEEG.gameObject.SetActive(false);
            m_Height.gameObject.SetActive(false);
            m_Latency.gameObject.SetActive(false);
            m_MarsAtlas.gameObject.SetActive(true);
            m_Broadman.gameObject.SetActive(true);
            ApplicationState.Module3D.OnDisplaySiteInformation.AddListener((siteInfo) =>
            {
                SiteInformationDisplayMode mode = siteInfo.Mode;
                if (mode != m_CurrentMode)
                {
                    m_CurrentMode = mode;
                    switch (mode)
                    {
                        case SiteInformationDisplayMode.IEEGNoAmplitude:
                            m_IEEG.gameObject.SetActive(false);
                            m_Height.gameObject.SetActive(false);
                            m_Latency.gameObject.SetActive(false);
                            m_MarsAtlas.gameObject.SetActive(true);
                            m_Broadman.gameObject.SetActive(true);
                            break;
                        case SiteInformationDisplayMode.IEEG:
                            m_IEEG.gameObject.SetActive(true);
                            m_Height.gameObject.SetActive(false);
                            m_Latency.gameObject.SetActive(false);
                            m_MarsAtlas.gameObject.SetActive(true);
                            m_Broadman.gameObject.SetActive(true);
                            break;
                        case SiteInformationDisplayMode.FMRI:
                            m_IEEG.gameObject.SetActive(false);
                            m_Height.gameObject.SetActive(false);
                            m_Latency.gameObject.SetActive(false);
                            m_MarsAtlas.gameObject.SetActive(false);
                            m_Broadman.gameObject.SetActive(false);
                            break;
                        case SiteInformationDisplayMode.CCEP:
                            m_IEEG.gameObject.SetActive(false);
                            m_Height.gameObject.SetActive(true);
                            m_Latency.gameObject.SetActive(true);
                            m_MarsAtlas.gameObject.SetActive(false);
                            m_Broadman.gameObject.SetActive(false);
                            break;
                        default:
                            break;
                    }
                }
                gameObject.SetActive(siteInfo.Enabled);
                if (siteInfo.Enabled)
                {
                    transform.position = siteInfo.Position;
                    m_SiteName.text = siteInfo.Site.Information.Name;
                    m_PatientDate.text = siteInfo.Site.Information.Patient.Date.ToString();
                    m_PatientName.text = siteInfo.Site.Information.Patient.Name;
                    m_PatientPlace.text = siteInfo.Site.Information.Patient.Place;
                    if (siteInfo.Mode == SiteInformationDisplayMode.CCEP)
                    {
                        m_Height.text = "Height: " + siteInfo.Height;
                        m_Latency.text = "Latency: " + siteInfo.Latency;
                    }
                    else if (siteInfo.Mode == SiteInformationDisplayMode.IEEG || siteInfo.Mode == SiteInformationDisplayMode.IEEGNoAmplitude)
                    {
                        if (siteInfo.Mode == SiteInformationDisplayMode.IEEG) m_IEEG.text = "IEEG: " + siteInfo.Amplitude;
                        if (siteInfo.Site)
                        {
                            string marsAtlasText = ApplicationState.Module3D.MarsAtlasIndex.FullName(siteInfo.Site.Information.MarsAtlasIndex);
                            if (marsAtlasText != "No_info" && marsAtlasText != "not found")
                            {
                                m_MarsAtlas.gameObject.SetActive(true);
                                m_MarsAtlas.text = "Mars Atlas: " + marsAtlasText;
                            }
                            else
                            {
                                m_MarsAtlas.gameObject.SetActive(false);
                            }
                            string broadmanText = ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(siteInfo.Site.Information.MarsAtlasIndex);
                            if (broadmanText != "No_info" && broadmanText != "not found")
                            {
                                m_Broadman.gameObject.SetActive(true);
                                m_Broadman.text = "Broadman: " + broadmanText;
                            }
                            else
                            {
                                m_Broadman.gameObject.SetActive(false);
                            }
                        }
                    }
                    ClampToCanvas();
                }
            });
        }
        #endregion

        #region Private Methods
        private void ClampToCanvas() // FIXME : high cost of performance
		{
            Vector3 l_pos = m_RectTransform.localPosition;
			Vector3 l_minPosition = m_Canvas.rect.min - m_RectTransform.rect.min;
			Vector3 l_maxPosition = m_Canvas.rect.max - m_RectTransform.rect.max;

            l_minPosition = new Vector3(l_minPosition.x + 30.0f, l_minPosition.y + 30.0f, l_minPosition.z);
            l_maxPosition = new Vector3(l_maxPosition.x - 30.0f, l_maxPosition.y - 30.0f, l_maxPosition.z);

            l_pos.x = Mathf.Clamp (m_RectTransform.localPosition.x, l_minPosition.x, l_maxPosition.x);
			l_pos.y = Mathf.Clamp (m_RectTransform.localPosition.y, l_minPosition.y, l_maxPosition.y);

            m_RectTransform.localPosition = l_pos;
		}
        #endregion
    }
}