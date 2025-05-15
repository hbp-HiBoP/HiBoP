using Cysharp.Threading.Tasks;
using System.Linq;

namespace HBP.UI.Module3D
{
    public class DisplayGraphSection : SiteToolSection
    {
        #region Public Methods
        public override void Initialize()
        {
        }
        public override async UniTask ApplyAsync()
        {
            await UniTask.SwitchToMainThread();
            Scene.OnRequestFilteredSitesGraph.Invoke(Sites);
        }
        #endregion
    }
}