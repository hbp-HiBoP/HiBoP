using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class DisplayGraphSection : SiteToolSection
    {
        #region Properties
        [SerializeField] private InputField m_NameInputField;
        #endregion

        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();
            Scene.OnRequestFilteredSitesGraph.Invoke(m_NameInputField.text, Sites);
        }
        public override void StoreSettings()
        {
            // No settings to store
        }
        public override void LoadSettings()
        {
            // No settings to load
        }
        #endregion
    }
}