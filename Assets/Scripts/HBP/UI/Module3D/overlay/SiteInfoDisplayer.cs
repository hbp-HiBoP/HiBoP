using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Small window to display information about the hovered site
    /// </summary>
    public class SiteInfoDisplayer : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// GameObject displaying information about iEEG activity of the hovered site
        /// </summary>
        [SerializeField] GameObject m_IEEG;
        /// <summary>
        /// GameObject displaying information about CCEP activity of the hovered site
        /// </summary>
        [SerializeField] GameObject m_CCEP;
        /// <summary>
        /// GameObject displaying information about the atlases of the hovered site
        /// </summary>
        [SerializeField] GameObject m_Tags;
        /// <summary>
        /// GameObject displaying information about the state of the hovered site
        /// </summary>
        [SerializeField] GameObject m_States;
        /// <summary>
        /// Displays the name of the site
        /// </summary>
        [SerializeField] Text m_SiteNameText;
        /// <summary>
        /// If this image is visible, that means the site is highlighted
        /// </summary>
        [SerializeField] Image m_IsHighlightedImage;
        /// <summary>
        /// If this image is visible, that means the site is blacklisted
        /// </summary>
        [SerializeField] Image m_IsBlackListedImage;
        /// <summary>
        /// Displays information about the patient
        /// </summary>
        [SerializeField] Text m_PatientText;
        /// <summary>
        /// Displays the amplitude of the iEEG activity
        /// </summary>
        [SerializeField] Text m_IEEGAmplitudeText;
        /// <summary>
        /// Displays the amplitude of the CCEP activity of the first spike
        /// </summary>
        [SerializeField] Text m_CCEPAmplitudeText;
        /// <summary>
        /// Displays the latency of the first spike
        /// </summary>
        [SerializeField] Text m_CCEPLatencyText;
        /// <summary>
        /// Displays the tags of the site
        /// </summary>
        [SerializeField] Text m_TagsText;
        /// <summary>
        /// Parent canvas of this object
        /// </summary>
        [SerializeField] RectTransform m_Canvas;

        /// <summary>
        /// Current selected mode to display the site information
        /// </summary>
        SiteInformationDisplayMode m_CurrentMode = SiteInformationDisplayMode.Anatomy;
        /// <summary>
        /// RectTransform of this object
        /// </summary>
        RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this object
        /// </summary>
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_IEEG.SetActive(false);
            m_CCEP.SetActive(false);
            m_Tags.SetActive(true);
            HBP3DModule.OnDisplaySiteInformation.AddListener((siteInfo) =>
            {
                SiteInformationDisplayMode mode = siteInfo.Mode;
                if (mode != m_CurrentMode)
                {
                    m_CurrentMode = mode;
                    switch (mode)
                    {
                        case SiteInformationDisplayMode.Anatomy:
                            m_IEEG.SetActive(false);
                            m_CCEP.SetActive(false);
                            m_Tags.SetActive(true);
                            m_States.SetActive(true);
                            break;
                        case SiteInformationDisplayMode.IEEG:
                            m_IEEG.SetActive(true);
                            m_CCEP.SetActive(false);
                            m_Tags.SetActive(true);
                            m_States.SetActive(true);
                            break;
                        case SiteInformationDisplayMode.CCEP:
                            m_IEEG.SetActive(false);
                            m_CCEP.SetActive(true);
                            m_Tags.SetActive(true);
                            m_States.SetActive(true);
                            break;
                        case SiteInformationDisplayMode.Light:
                            m_IEEG.SetActive(false);
                            m_CCEP.SetActive(false);
                            m_Tags.SetActive(false);
                            m_States.SetActive(false);
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
                        case SiteInformationDisplayMode.Anatomy:
                            SetStates(siteInfo.Site);
                            SetTags(siteInfo);
                            break;
                        case SiteInformationDisplayMode.IEEG:
                            SetIEEG(siteInfo);
                            SetStates(siteInfo.Site);
                            SetTags(siteInfo);
                            break;
                        case SiteInformationDisplayMode.CCEP:
                            SetCCEP(siteInfo);
                            SetStates(siteInfo.Site);
                            SetTags(siteInfo);
                            break;
                        case SiteInformationDisplayMode.Light:
                            break;
                    }
                    ClampToCanvas();
                }
                gameObject.SetActive(siteInfo.Enabled);
            });
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Clamp this object to the parent canvas
        /// </summary>
        void ClampToCanvas()
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
        /// <summary>
        /// Set the position of this object on the screen
        /// </summary>
        /// <param name="siteInfo">Information about how to display the information of the site</param>
        void SetPosition(Core.Object3D.SiteInfo siteInfo)
        {
            transform.position = siteInfo.Position + new Vector3(0, -20, 0);
        }
        /// <summary>
        /// Set the site information (name)
        /// </summary>
        /// <param name="site">Site to display information of</param>
        void SetSite(Core.Object3D.Site site)
        {
            m_SiteNameText.text = site.Information.Name;
        }
        /// <summary>
        /// Set the patient information (name, place, date)
        /// </summary>
        /// <param name="patient">Patient to display information of</param>
        void SetPatient(Core.Data.Patient patient)
        {
            m_PatientText.text = patient.CompleteName;
        }
        /// <summary>
        /// Set the states of the site (highlighted, blacklisted)
        /// </summary>
        /// <param name="site">Site to display information of</param>
        void SetStates(Core.Object3D.Site site)
        {
            m_IsBlackListedImage.gameObject.SetActive(site.State.IsBlackListed);
            m_IsHighlightedImage.gameObject.SetActive(site.State.IsHighlighted);
        }
        /// <summary>
        /// Set the CCEP values of the site (amplitude, latency)
        /// </summary>
        /// <param name="siteInfo">Information about how to display the information of the site</param>
        void SetCCEP(Core.Object3D.SiteInfo siteInfo)
        {
            m_CCEPAmplitudeText.text = siteInfo.CCEPAmplitude;
            m_CCEPLatencyText.text = siteInfo.CCEPLatency;
        }
        /// <summary>
        /// Set the iEEG values of the site (amplitude)
        /// </summary>
        /// <param name="siteInfo">Information about how to display the information of the site</param>
        void SetIEEG(Core.Object3D.SiteInfo siteInfo)
        {
            string unit = siteInfo.IEEGUnit;
            if (unit == "microV") unit = "mV";
            if (unit != string.Empty) unit = " (" + unit + ")";
            m_IEEGAmplitudeText.text = siteInfo.IEEGAmplitude + unit;      
        }
        /// <summary>
        /// Set the atlases of the site (Mars atlas, Brodmann, Freesurfer)
        /// </summary>
        /// <param name="siteInfo">Information about how to display the information of the site</param>
        void SetTags(Core.Object3D.SiteInfo siteInfo)
        {
            if (siteInfo.Site && siteInfo.Site.Information.SiteData.Tags.Count > 0)
            {
                System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                foreach (var tag in siteInfo.Site.Information.SiteData.Tags)
                {
                    stringBuilder.Append(string.Format("\t• <b>{0}</b>: {1}\n", tag.Tag.Name, tag.DisplayableValue));
                }
                m_TagsText.text = stringBuilder.Remove(stringBuilder.Length - 1, 1).ToString();
                m_Tags.SetActive(true);
            }
            else
            {
                m_Tags.SetActive(false);
            }
        }
        #endregion
    }
}