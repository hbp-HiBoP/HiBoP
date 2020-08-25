using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class EpochingPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private Toggle m_AlreadyEpoched;
        [SerializeField] private Toggle m_NeedsEpoching;

        [SerializeField] private RectTransform m_EpochingPanel;
        [SerializeField] private InputField m_Codes;
        [SerializeField] private RangeSlider m_Window;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_Window.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_Window.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_Window.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_Window.Values = new Vector2(-500, 500);
        }
        #endregion
    }
}