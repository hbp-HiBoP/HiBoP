using Cysharp.Threading.Tasks;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class DisplayGraphSection : SiteToolSection
    {
        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();
            Scene.OnRequestFilteredSitesGraph.Invoke(Sites);
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