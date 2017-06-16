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
        [SerializeField]
        private Text m_FullName;

        [SerializeField]
        private Text m_IEEG;

        [SerializeField]
        private Text m_Time;

        [SerializeField]
        private Text m_Height;

        [SerializeField]
        private Text m_Latency;

        [SerializeField]
        private Text m_MarsAtlas;

        [SerializeField]
        private Text m_Broadman;

        private RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            ApplicationState.Module3D.OnDisplaySiteInformation.AddListener((siteInfo) =>
            {
                gameObject.SetActive(siteInfo.enabled);
                if (siteInfo.enabled)
                {
                    transform.position = siteInfo.position;
                    m_FullName.text = siteInfo.name;
                    if (siteInfo.isFMRI)
                    {
                        m_IEEG.gameObject.SetActive(false);
                        m_Time.gameObject.SetActive(false);
                        m_Height.gameObject.SetActive(false);
                        m_Latency.gameObject.SetActive(false);
                        m_MarsAtlas.gameObject.SetActive(false);
                        m_Broadman.gameObject.SetActive(false);
                        return;
                    }
                    else if (siteInfo.displayLatencies)
                    {
                        m_IEEG.gameObject.SetActive(false);
                        m_Time.gameObject.SetActive(false);
                        m_Height.gameObject.SetActive(true);
                        m_Latency.gameObject.SetActive(true);
                        m_MarsAtlas.gameObject.SetActive(false);
                        m_Broadman.gameObject.SetActive(false);
                        m_Height.text = "Height: " + siteInfo.height;
                        m_Latency.text = "Latency: " + siteInfo.latency;
                        return;
                    }
                    m_IEEG.gameObject.SetActive(true);
                    m_Time.gameObject.SetActive(true);
                    m_Height.gameObject.SetActive(false);
                    m_Latency.gameObject.SetActive(false);
                    m_MarsAtlas.gameObject.SetActive(true);
                    m_Broadman.gameObject.SetActive(true);
                    m_IEEG.text = "IEEG: " + siteInfo.amplitude;
                    m_Time.text = "NYI: GET_TIME";
                    
                    if (siteInfo.site)
                    {
                        m_MarsAtlas.text = "Mars Atlas: " + GlobalGOPreloaded.MarsAtlasIndex.FullName(siteInfo.site.Information.MarsAtlasIndex);
                        m_Broadman.text = "Broadman: " + GlobalGOPreloaded.MarsAtlasIndex.BroadmanArea(siteInfo.site.Information.MarsAtlasIndex);
                    }
                }
            });
        }
        #endregion
    }
}