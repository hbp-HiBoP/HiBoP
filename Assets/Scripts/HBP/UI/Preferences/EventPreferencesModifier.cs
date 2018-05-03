using System;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class EventPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_PositionAveragingDropdown;
        #endregion

        #region Public Methods
        public void Set()
        {
            Data.Preferences.EventPreferences preferences = ApplicationState.UserPreferences.Data.Event;

            string[] averagingType = Enum.GetNames(typeof(Data.Enums.AveragingType));
            m_PositionAveragingDropdown.ClearOptions();
            foreach (string type in averagingType)
            {
                m_PositionAveragingDropdown.options.Add(new Dropdown.OptionData(type));
            }
            m_PositionAveragingDropdown.value = (int)preferences.PositionAveraging;
            m_PositionAveragingDropdown.RefreshShownValue();
        }

        public void Save()
        {
            Data.Preferences.EventPreferences preferences = ApplicationState.UserPreferences.Data.Event;

            preferences.PositionAveraging = (Data.Enums.AveragingType) m_PositionAveragingDropdown.value;
        }
        #endregion
    }
}