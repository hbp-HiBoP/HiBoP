using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class SiteLabelItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_LabelText;
        [SerializeField] private Button m_RemoveLabelButton;
        #endregion

        #region Public Methods
        public void Initialize(HBP.Module3D.Site site, string label)
        {
            m_LabelText.text = label;
            m_RemoveLabelButton.onClick.AddListener(() =>
            {
                site.State.RemoveLabel(label);
            });
        }
        #endregion
    }
}