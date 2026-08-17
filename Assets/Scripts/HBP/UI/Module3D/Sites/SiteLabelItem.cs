using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Module3D
{
    public class SiteLabelItem : MonoBehaviour
    {
        #region Properties

        public string Label { get; private set; }
        [SerializeField] private Text m_LabelText;
        [SerializeField] private Button m_RemoveLabelButton;

        #endregion

        #region Events

        public GenericEvent<string> OnRemoveLabel = new();

        #endregion

        #region Public Methods

        public void Initialize(string label)
        {
            Label = label;
            m_LabelText.text = label;
            m_RemoveLabelButton.onClick.AddListener(() => { OnRemoveLabel.Invoke(label); });
        }

        #endregion
    }
}
