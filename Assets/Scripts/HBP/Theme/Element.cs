using System.Linq;
using UnityEngine;

namespace HBP.Theme
{
    [CreateAssetMenu(menuName = "Theme/Element")]
    public class Element : ScriptableObject
    {
        #region Properties
        public State DefaultState;
        public SettingByState[] SettingsByState;
        #endregion

        #region Public Methods
        public State Set(GameObject gameObject)
        {
            return Set(gameObject, DefaultState);
        }
        public State Set(GameObject gameObject, State state)
        {
            SettingByState settingsByState = SettingsByState.FirstOrDefault((s) => s.State == state);
            if(settingsByState != null)
            {
                foreach (var setting in settingsByState.Settings)
                {
                    if (setting) setting.Set(gameObject);
                }
                return state;
            }
            return null;
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