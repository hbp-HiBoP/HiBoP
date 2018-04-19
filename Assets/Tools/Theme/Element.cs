using System.Linq;
using UnityEngine;

namespace NewTheme
{
    [CreateAssetMenu(menuName = "Theme/Element")]
    public class Element : ScriptableObject
    {
        #region Properties
        public State Default;
        public SettingByState[] SettingsByState;
        #endregion

        #region Public Methods
        public void Set(GameObject gameObject)
        {
            Set(gameObject, Default);
        }
        public void Set(GameObject gameObject, State state)
        {
            SettingByState settingsByState = SettingsByState.FirstOrDefault((s) => s.State == state);
            if(settingsByState != null)
            {
                foreach (var setting in settingsByState.Settings)
                {
                    if (setting) setting.Set(gameObject);
                }
            }
        }
        #endregion

        #region Private Class
        [System.Serializable]
        public class SettingByState
        {
            public State State;
            public Settings[] Settings;
        }
        #endregion
    }
}