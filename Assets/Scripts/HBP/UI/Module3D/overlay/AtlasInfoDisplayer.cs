using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Small window to display information about the hovered atlas area
    /// </summary>
    public class AtlasInfoDisplayer : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Area to display MarsAtlas information
        /// </summary>
        [SerializeField] RectTransform m_MarsAtlasArea;
        /// <summary>
        /// Display the name of the hovered MarsAtlas area
        /// </summary>
        [SerializeField] Text m_MarsAtlasNameText;
        /// <summary>
        /// Display all the parent areas of the hovered MarsAtlas area
        /// </summary>
        [SerializeField] Text m_HemisphereText;
        /// <summary>
        /// Display the area label of the hovered MarsAtlas area
        /// </summary>
        [SerializeField] Text m_LobeText;
        /// <summary>
        /// Display the status of the hovered MarsAtlas area
        /// </summary>
        [SerializeField] Text m_NameFSText;
        /// <summary>
        /// Display the DOI of the hovered MarsAtlas area
        /// </summary>
        [SerializeField] Text m_FullNameText;

        /// <summary>
        /// Area to display JuBrain information
        /// </summary>
        [SerializeField] RectTransform m_JuBrainArea;
        /// <summary>
        /// Display the name of the hovered JuBrain area
        /// </summary>
        [SerializeField] Text m_JuBrainNameText;
        /// <summary>
        /// Display all the parent areas of the hovered JuBrain area
        /// </summary>
        [SerializeField] Text m_LocationText;
        /// <summary>
        /// Display the area label of the hovered JuBrain area
        /// </summary>
        [SerializeField] Text m_AreaLabelText;
        /// <summary>
        /// Display the status of the hovered JuBrain area
        /// </summary>
        [SerializeField] Text m_StatusText;
        /// <summary>
        /// Display the DOI of the hovered JuBrain area
        /// </summary>
        [SerializeField] Text m_DOIText;

        /// <summary>
        /// Parent canvas of this object
        /// </summary>
        [SerializeField] RectTransform m_Canvas;
        /// <summary>
        /// Reference to the RectTransform of this object
        /// </summary>
        RectTransform m_RectTransform;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this object (add a listener to know when to display it)
        /// </summary>
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            Module3DMain.OnDisplayAtlasInformation.AddListener((atlasInfo) =>
            {
                if (atlasInfo.Enabled)
                {
                    transform.position = atlasInfo.Position + new Vector3(0, -20, 0);
                    switch (atlasInfo.Type)
                    {
                        case AtlasInfo.AtlasType.MarsAtlas:
                            m_MarsAtlasArea.gameObject.SetActive(true);
                            m_JuBrainArea.gameObject.SetActive(false);
                            m_MarsAtlasNameText.text = atlasInfo.Information1;
                            m_HemisphereText.text = atlasInfo.Information2;
                            m_LobeText.text = atlasInfo.Information3;
                            m_NameFSText.text = atlasInfo.Information4;
                            m_FullNameText.text = atlasInfo.Information5;
                            break;
                        case AtlasInfo.AtlasType.JuBrainAtlas:
                            m_MarsAtlasArea.gameObject.SetActive(false);
                            m_JuBrainArea.gameObject.SetActive(true);
                            m_JuBrainNameText.text = atlasInfo.Information1;
                            m_LocationText.text = atlasInfo.Information2;
                            m_AreaLabelText.text = atlasInfo.Information3;
                            m_StatusText.text = atlasInfo.Information4;
                            m_DOIText.text = atlasInfo.Information5;
                            break;
                    }
                    ClampToCanvas();
                }
                gameObject.SetActive(atlasInfo.Enabled);
            });
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Clamp this object to the parent canvas
        /// </summary>
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
