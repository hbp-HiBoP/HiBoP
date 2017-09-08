using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public abstract class Toolbar : MonoBehaviour
    {
        public enum UpdateToolbarType { Scene, Column, View }

        #region Properties
        protected UnityAction<HBP.Module3D.Mode> m_ChangeModeAction;
        /// <summary>
        /// List of the tools of the toolbar
        /// </summary>
        protected List<Tools.Tool> m_Tools = new List<Tools.Tool>();
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected abstract void AddTools();
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
            
            ApplicationState.Module3D.OnRequestUpdateInUI.AddListener(() =>
            {
                UpdateInteractableButtons();
                UpdateButtonsStatus(UpdateToolbarType.Scene);
            });
            
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.Initialize();
            }
        }
        /// <summary>
        /// Set the toolbar elements to their default state
        /// </summary>
        protected virtual void DefaultState()
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.DefaultState();
            }
        }
        /// <summary>
        /// Callback when the selected scene is changed
        /// </summary>
        protected void OnChangeScene()
        {
            m_Tools.ForEach((t) => t.ListenerLock = true);
            if (m_ChangeModeAction != null) ApplicationState.Module3D.SelectedScene.ModesManager.OnChangeMode.RemoveListener(m_ChangeModeAction);
            m_ChangeModeAction = new UnityAction<HBP.Module3D.Mode>((mode) => UpdateInteractableButtons());
            ApplicationState.Module3D.SelectedScene.ModesManager.OnChangeMode.AddListener(m_ChangeModeAction);
            UpdateInteractableButtons();
            UpdateButtonsStatus(UpdateToolbarType.Scene);
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        /// <summary>
        /// Callback when the selected column is changed
        /// </summary>
        protected void OnChangeColumn()
        {
            m_Tools.ForEach((t) => t.ListenerLock = true);
            UpdateInteractableButtons();
            UpdateButtonsStatus(UpdateToolbarType.Column);
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        /// <summary>
        /// Callback when the selected view is changed
        /// </summary>
        protected void OnChangeView()
        {
            m_Tools.ForEach((t) => t.ListenerLock = true);
            UpdateInteractableButtons();
            UpdateButtonsStatus(UpdateToolbarType.View);
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        /// <summary>
        /// Update the interactable buttons of the toolbar
        /// </summary>
        protected virtual void UpdateInteractableButtons()
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.UpdateInteractable();
            }
        }
        /// <summary>
        /// Change the status of the toolbar elements according to the selected scene parameters
        /// </summary>
        protected virtual void UpdateButtonsStatus(UpdateToolbarType type)
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.UpdateStatus(type);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public void Initialize()
        {
            AddTools();
            m_Tools.ForEach((t) => t.ListenerLock = true);
            AddListeners();
            DefaultState();
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        /// <summary>
        /// Called when showing this toolbar
        /// </summary>
        public virtual void ShowToolbarCallback()
        {

        }
        /// <summary>
        /// Called when hiding this toolbar
        /// </summary>
        public virtual void HideToolbarCallback()
        {

        }
        #endregion
    }
}