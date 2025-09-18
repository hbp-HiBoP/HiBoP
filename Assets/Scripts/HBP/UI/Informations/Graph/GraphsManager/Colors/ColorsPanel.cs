using Cysharp.Threading.Tasks;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class ColorsPanel : MonoBehaviour
    {
        #region Properties
        public async void OpenUserPreferences()
        {
            var window = WindowsManager.OpenModifier(PersistentDataManager.UserPreferences, null);
            var navigator = window.GetComponent<ToggleNavigator>();
            navigator.Navigate("Visualization");
            await UniTask.WaitForEndOfFrame();
            navigator.Navigate("Visualization_Graph");
        }
        #endregion
    }
}