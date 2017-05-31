using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public abstract class Toolbar : MonoBehaviour
    {
        protected enum UpdateToolbarType { Scene, Column, View }

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
        protected virtual void AddListeners()
        {
            ApplicationState.Module3D.OnSelectScene.AddListener((scene) => OnChangeScene());

            ApplicationState.Module3D.OnRemoveScene.AddListener((scene) =>
            {
                if (scene == ApplicationState.Module3D.SelectedScene)
                {
                    DefaultState();
                }
            });

            ApplicationState.Module3D.OnSelectColumn.AddListener((column) => OnChangeColumn());

            ApplicationState.Module3D.OnSelectView.AddListener((column) => OnChangeView());
        }
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
            UpdateButtonsStatus(UpdateToolbarType.Scene);
            ChangeSceneLock = false;
        }
        /// <summary>
        /// Callback when the selected column is changed
        /// </summary>
        protected void OnChangeColumn()
        {
            ChangeSceneLock = true;
            UpdateInteractableButtons();
            UpdateButtonsStatus(UpdateToolbarType.Column);
            ChangeSceneLock = false;
        }
        /// <summary>
        /// Callback when the selected view is changed
        /// </summary>
        protected void OnChangeView()
        {
            ChangeSceneLock = true;
            UpdateInteractableButtons();
            UpdateButtonsStatus(UpdateToolbarType.View);
            ChangeSceneLock = false;
        }
        /// <summary>
        /// Update the interactable buttons of the toolbar
        /// </summary>
        protected abstract void UpdateInteractableButtons();
        /// <summary>
        /// Change the status of the toolbar elements according to the selected scene parameters
        /// </summary>
        protected abstract void UpdateButtonsStatus(UpdateToolbarType type);
        #endregion
    }
}