using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    public abstract class Toolbar : MonoBehaviour
    {
        #region Properties
        protected ToolbarMenu m_ToolbarMenu;
        /// <summary>
        /// List of the tools of the toolbar
        /// </summary>
        protected List<Tools.Tool> m_Tools = new List<Tools.Tool>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ToolbarMenu = FindObjectOfType<ToolbarMenu>();
        }
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
            ApplicationState.Module3D.OnRemoveScene.AddListener((scene) =>
            {
                if (scene == ApplicationState.Module3D.SelectedScene)
                {
                    m_Tools.ForEach((t) => t.ListenerLock = true);
                    DefaultState();
                    m_Tools.ForEach((t) => t.ListenerLock = false);
                }
            });

            ApplicationState.Module3D.OnMinimizeScene.AddListener((scene) =>
            {
                if (scene == ApplicationState.Module3D.SelectedScene)
                {
                    m_Tools.ForEach((t) => t.ListenerLock = true);
                    DefaultState();
                    m_Tools.ForEach((t) => t.ListenerLock = false);
                }
            });

            ApplicationState.Module3D.OnSelectScene.AddListener(OnChangeScene);
            ApplicationState.Module3D.OnSelectColumn.AddListener(OnChangeColumn);
            ApplicationState.Module3D.OnSelectView.AddListener(OnChangeView);
            
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
        protected void OnChangeScene(Base3DScene scene)
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.SelectedScene = scene;
            }
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Callback when the selected column is changed
        /// </summary>
        protected void OnChangeColumn(Column3D column)
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.SelectedColumn = column;
            }
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Callback when the selected view is changed
        /// </summary>
        protected void OnChangeView(View3D view)
        {
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.SelectedView = view;
            }
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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
        /// <summary>
        /// Update all the tools
        /// </summary>
        public void UpdateToolbar()
        {
            m_Tools.ForEach((t) => t.ListenerLock = true);
            foreach (Tools.Tool tool in m_Tools)
            {
                tool.UpdateTool();
            }
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        #endregion
    }
}