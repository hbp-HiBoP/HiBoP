using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class AtlasInfoDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_LocationText;
        [SerializeField] Text m_AreaLabelText;
        [SerializeField] Text m_StatusText;
        [SerializeField] Text m_DOIText;

        [SerializeField] RectTransform m_Canvas;
        RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            ApplicationState.Module3D.OnDisplayAtlasInformation.AddListener((atlasInfo) =>
            {
                if (atlasInfo.Enabled)
                {
                    transform.position = atlasInfo.Position + new Vector3(0, -20, 0);
                    m_NameText.text = atlasInfo.Name;
                    m_LocationText.text = atlasInfo.Location;
                    m_AreaLabelText.text = atlasInfo.AreaLabel;
                    m_StatusText.text = atlasInfo.Status;
                    m_DOIText.text = atlasInfo.DOI;
                    ClampToCanvas();
                }
                gameObject.SetActive(atlasInfo.Enabled);
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

            l_pos.x = Mathf.Clamp(m_RectTransform.localPosition.x, l_minPosition.x, l_maxPosition.x);
            l_pos.y = Mathf.Clamp(m_RectTransform.localPosition.y, l_minPosition.y, l_maxPosition.y);

            m_RectTransform.localPosition = l_pos;
        }
        #endregion
    }
}
