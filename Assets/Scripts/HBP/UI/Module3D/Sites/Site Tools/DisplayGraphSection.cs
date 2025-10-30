using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class DisplayGraphSection : SiteToolSection
    {
        #region Properties
        [SerializeField] private InputField m_NameInputField;

        static string m_NameInputFieldValue;
        #endregion

        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();
            Scene.OnRequestFilteredSitesGraph.Invoke(m_NameInputField.text, Sites);
        }
        public override void StoreSettings()
        {
            m_NameInputFieldValue = m_NameInputField.text;
        }
        public override void LoadSettings()
        {
            m_NameInputField.text = m_NameInputFieldValue;
        }
        #endregion
    }
}