using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public abstract class Toolbar : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Lock to prevent the calls to the listeners when only changing the selected scene
        /// </summary>
        public bool ChangeSceneLock { get; set; }
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected abstract void FindButtons();
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected abstract void AddListeners();
        /// <summary>
        /// Set the toolbar elements to their default state
        /// </summary>
        protected abstract void DefaultState();
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        protected void Initialize()
        {
            FindButtons();
            AddListeners();
            DefaultState();
        }
        /// <summary>
        /// Callback when the selected scene is changed
        /// </summary>
        protected void OnChangeScene()
        {
            ChangeSceneLock = true;
            ApplicationState.Module3D.SelectedScene.ModesManager.OnChangeMode.AddListener((mode) => UpdateInteractableButtons()); // maybe FIXME : problem with infinite number of listeners ?
            UpdateInteractableButtons();
            UpdateButtonsStatus();
            ChangeSceneLock = false;
        }
        /// <summary>
        /// Update the interactable buttons of the toolbar
        /// </summary>
        protected abstract void UpdateInteractableButtons();
        /// <summary>
        /// Change the status of the toolbar elements according to the selected scene parameters
        /// </summary>
        protected abstract void UpdateButtonsStatus();
        #endregion
    }
}