using System;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Preferences
{
    public class EventPreferencesModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_PositionAveragingDropdown;

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;

                m_PositionAveragingDropdown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {
            Data.Preferences.EventPreferences preferences = ApplicationState.UserPreferences.Data.Event;

            m_PositionAveragingDropdown.Set(typeof(Data.Enums.AveragingType), (int)preferences.PositionAveraging);
        }
        public void Save()
        {
            Data.Preferences.EventPreferences preferences = ApplicationState.UserPreferences.Data.Event;
            preferences.PositionAveraging = (Data.Enums.AveragingType) m_PositionAveragingDropdown.value;
        }
        #endregion
    }
}