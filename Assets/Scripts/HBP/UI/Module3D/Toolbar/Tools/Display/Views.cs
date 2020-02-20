using HBP.Module3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class Views : Tool
    {
        #region Properties
        /// <summary>
        /// Add a new view line
        /// </summary>
        [SerializeField] private Button m_Add;
        /// <summary>
        /// Remove the last view line
        /// </summary>
        [SerializeField] private Button m_Remove;
        #endregion

        #region Events
        /// <summary>
        /// Event called when either button has been pressed
        /// </summary>
        public UnityEvent OnClick = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Add.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.AddViewLine();
                OnClick.Invoke();
            });

            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.RemoveViewLine();
                OnClick.Invoke();
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Add.interactable = false;
            m_Remove.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool canAddView = SelectedScene.ViewLineNumber < HBP3DModule.MAXIMUM_VIEW_NUMBER;
            bool canRemoveView = SelectedScene.ViewLineNumber > 1;

            m_Add.interactable = canAddView;
            m_Remove.interactable = canRemoveView;
        }
        #endregion
    }
}