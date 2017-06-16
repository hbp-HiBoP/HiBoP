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

        [SerializeField]
        private RectTransform m_Canvas;

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
                    }
                    else
                    {
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
                            m_MarsAtlas.text = "Mars Atlas: " + ApplicationState.Module3D.MarsAtlasIndex.FullName(siteInfo.site.Information.MarsAtlasIndex);
                            m_Broadman.text = "Broadman: " + ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(siteInfo.site.Information.MarsAtlasIndex);
                        }
                    }
                    ClampToCanvas();
                }
            });
        }
        #endregion

        #region Private Methods
        private void ClampToCanvas() 
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