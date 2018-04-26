using System;
using UnityEngine.Events;

namespace HBP.UI.Preferences
{
    public class UserPreferences : Window
    {
        #region Properties
        public UnityEvent OnSave;
        public UnityEvent OnSetWindow;
        #endregion

        #region Public Methods
        public void Save()
        {
            OnSave.Invoke();
            Close();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
        }


        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            OnSetWindow.Invoke();
        }
        #endregion
    }
}