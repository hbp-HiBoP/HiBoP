using System;
using Tools.Unity;
using UnityEngine.Events;

namespace HBP.UI.Preferences
{
    public class UserPreferences : SavableWindow
    {
        #region Properties
        public UnityEvent OnSetWindow;
        #endregion

        #region Public Methods
        public override  void Save()
        {
            ClassLoaderSaver.SaveToJSon(ApplicationState.UserPreferences, Data.Preferences.UserPreferences.PATH, true);
            base.Save();
        }
        public override void Close()
        {
            base.Close();
            Theme.Theme.UpdateThemeElements(ApplicationState.UserPreferences.Theme);
        }


        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            OnSetWindow.Invoke();
        }
        protected override void SetInteractable(bool interactable)
        {
        }
        #endregion
    }
}