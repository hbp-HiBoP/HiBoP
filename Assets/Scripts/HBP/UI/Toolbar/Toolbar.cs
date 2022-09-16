using HBP.Data.Module3D;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Toolbar
{
    public abstract class Toolbar : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Reference to the toolbar menu
        /// </summary>
        protected ToolbarMenu m_ToolbarMenu;
        /// <summary>
        /// List of the tools of the toolbar
        /// </summary>
        protected List<Tool> m_Tools = new List<Tool>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ToolbarMenu = FindObjectOfType<ToolbarMenu>();
        }
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected abstract void AddTools();
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected virtual void AddListeners()
        {
            Module3DMain.OnRemoveScene.AddListener((scene) =>
            {
                if (scene == Module3DMain.SelectedScene)
                {
                    m_Tools.ForEach((t) => t.ListenerLock = true);
                    DefaultState();
                    m_Tools.ForEach((t) => t.ListenerLock = false);
                }
            });

            Module3DMain.OnMinimizeScene.AddListener((scene) =>
            {
                if (scene == Module3DMain.SelectedScene)
                {
                    m_Tools.ForEach((t) => t.ListenerLock = true);
                    DefaultState();
                    m_Tools.ForEach((t) => t.ListenerLock = false);
                }
            });

            Module3DMain.OnSelectScene.AddListener(OnChangeScene);
            Module3DMain.OnSelectColumn.AddListener(OnChangeColumn);
            Module3DMain.OnSelectView.AddListener(OnChangeView);
            
            foreach (Tool tool in m_Tools)
            {
                tool.Initialize();
            }
        }
        /// <summary>
        /// Set the toolbar elements to their default state
        /// </summary>
        protected virtual void DefaultState()
        {
            foreach (Tool tool in m_Tools)
            {
                tool.DefaultState();
            }
        }
        /// <summary>
        /// Callback when the selected scene is changed
        /// </summary>
        /// <param name="scene">Scene that has been selected</param>
        protected void OnChangeScene(Base3DScene scene)
        {
            foreach (Tool tool in m_Tools)
            {
                tool.SelectedScene = scene;
            }
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Callback when the selected column is changed
        /// </summary>
        /// <param name="column">Column that has been selected</param>
        protected void OnChangeColumn(Column3D column)
        {
            foreach (Tool tool in m_Tools)
            {
                tool.SelectedColumn = column;
            }
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Callback when the selected view is changed
        /// </summary>
        /// <param name="view">View that has been selected</param>
        protected void OnChangeView(View3D view)
        {
            foreach (Tool tool in m_Tools)
            {
                tool.SelectedView = view;
            }
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
            foreach (Tool tool in m_Tools)
            {
                tool.UpdateTool();
            }
            m_Tools.ForEach((t) => t.ListenerLock = false);
        }
        #endregion
    }
}