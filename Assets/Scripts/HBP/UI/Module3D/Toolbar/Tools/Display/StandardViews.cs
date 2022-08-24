using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class StandardViews : Tool
    {
        #region Properties
        /// <summary>
        /// Button to set up the standard views
        /// </summary>
        [SerializeField] private Button m_Button;
        #endregion

        #region Events
        /// <summary>
        /// Event called when the button has been clicked
        /// </summary>
        public UnityEvent OnClick = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                if (ListenerLock) return;
                
                while (SelectedScene.ViewLineNumber > 3)
                {
                    SelectedScene.RemoveViewLine();
                }
                while (SelectedScene.ViewLineNumber < 3)
                {
                    SelectedScene.AddViewLine();
                }
                foreach (Column3D column in SelectedScene.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.Default();
                    }
                }
                OnClick.Invoke();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }
        #endregion
    }
}