using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteInfoDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_IEEG;
        [SerializeField] GameObject m_CCEP;
        [SerializeField] GameObject m_Atlas;
        [SerializeField] Text m_SiteNameText;
        [SerializeField] Image m_IsExcludedImage;
        [SerializeField] Image m_IsHighlightedImage;
        [SerializeField] Image m_IsBlackListedImage;
        [SerializeField] Image m_IsMarkedImage;
        [SerializeField] Image m_IsSuspiciousImage;
        [SerializeField] Text m_PatientText;
        [SerializeField] Text m_IEEGAmplitudeText;
        [SerializeField] Text m_CCEPAmplitudeText;
        [SerializeField] Text m_CCEPLatencyText;
        [SerializeField] Text m_MarsAtlasText;
        [SerializeField] Text m_BroadmanText;
        [SerializeField] RectTransform m_Canvas;

        Data.Enums.SiteInformationDisplayMode m_CurrentMode = Data.Enums.SiteInformationDisplayMode.Anatomy;
        RectTransform m_RectTransform;
        Color m_DisableColor = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_IEEG.SetActive(false);
            m_CCEP.SetActive(false);
            m_Atlas.SetActive(true);
            ApplicationState.Module3D.OnDisplaySiteInformation.AddListener((siteInfo) =>
            {
                Data.Enums.SiteInformationDisplayMode mode = siteInfo.Mode;
                if (mode != m_CurrentMode)
                {
                    m_CurrentMode = mode;
                    switch (mode)
                    {
                        case Data.Enums.SiteInformationDisplayMode.Anatomy:
                            m_IEEG.SetActive(false);
                            m_CCEP.SetActive(false);
                            m_Atlas.SetActive(true);
                            break;
                        case Data.Enums.SiteInformationDisplayMode.IEEG:
                            m_IEEG.SetActive(true);
                            m_CCEP.SetActive(false);
                            m_Atlas.SetActive(true);
                            break;
                        case Data.Enums.SiteInformationDisplayMode.CCEP:
                            m_IEEG.SetActive(false);
                            m_CCEP.SetActive(true);
                            m_Atlas.SetActive(false);
                            break;
                    }
                }
                if (siteInfo.Enabled)
                {
                    SetPosition(siteInfo);
                    SetSite(siteInfo.Site);
                    SetPatient(siteInfo.Site.Information.Patient);
                    switch (siteInfo.Mode)
                    {
                        case Data.Enums.SiteInformationDisplayMode.Anatomy:
                            SetAtlas(siteInfo);
                            break;
                        case Data.Enums.SiteInformationDisplayMode.IEEG:
                            SetIEEG(siteInfo);
                            SetAtlas(siteInfo);
                            break;
                        case Data.Enums.SiteInformationDisplayMode.CCEP:
                            SetCCEP(siteInfo);
                            break;
                    }
                    ClampToCanvas();
                }
                gameObject.SetActive(siteInfo.Enabled);
            });
        }
        #endregion

        #region Private Methods
        void ClampToCanvas() // FIXME : high cost of performance
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
        void SetPosition(HBP.Module3D.SiteInfo siteInfo)
        {
            transform.position = siteInfo.Position + new Vector3(0, -20, 0);
        }
        void SetSite(HBP.Module3D.Site site)
        {
            m_SiteNameText.text = site.Information.ChannelName;
            m_IsMarkedImage.gameObject.SetActive(site.State.IsMarked);
            m_IsBlackListedImage.gameObject.SetActive(site.State.IsBlackListed);
            m_IsHighlightedImage.gameObject.SetActive(site.State.IsHighlighted);
            m_IsSuspiciousImage.gameObject.SetActive(site.State.IsSuspicious);
            m_IsExcludedImage.gameObject.SetActive(site.State.IsExcluded);
        }
        void SetPatient(Data.Patient patient)
        {
            m_PatientText.text = patient.CompleteName;
        }
        void SetCCEP(HBP.Module3D.SiteInfo siteInfo)
        {
            m_CCEPAmplitudeText.text = siteInfo.CCEPAmplitude;
            m_CCEPLatencyText.text = siteInfo.CCEPLatency;
        }
        void SetIEEG(HBP.Module3D.SiteInfo siteInfo)
        {
            string unit = siteInfo.IEEGUnit;
            if (unit == "microV") unit = "mV";
            if (unit != string.Empty) unit = " (" + unit + ")";
            m_IEEGAmplitudeText.text = siteInfo.IEEGAmplitude + unit;      
        }
        void SetAtlas(HBP.Module3D.SiteInfo siteInfo)
        {
            if (siteInfo.Site)
            {
                string marsAtlasText = ApplicationState.Module3D.MarsAtlasIndex.FullName(siteInfo.Site.Information.MarsAtlasIndex);
                if (marsAtlasText != "No_info" && marsAtlasText != "not found")
                {
                    m_MarsAtlasText.transform.parent.gameObject.SetActive(true);
                    m_MarsAtlasText.text = marsAtlasText;
                }
                else
                {
                    m_MarsAtlasText.transform.parent.gameObject.SetActive(false);
                }
                string broadmanText = ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(siteInfo.Site.Information.MarsAtlasIndex);
                if (broadmanText != "No_info" && broadmanText != "not found")
                {
                    m_BroadmanText.transform.parent.gameObject.SetActive(true);
                    m_BroadmanText.text = broadmanText;
                }
                else
                {
                    m_BroadmanText.transform.parent.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}