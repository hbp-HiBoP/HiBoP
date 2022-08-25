using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class DefaultView : Tool
    {
        #region Properties
        /// <summary>
        /// Button to trigger the default views (camera orientation and distance)
        /// </summary>
        [SerializeField] private Button m_Button;
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

                int lineID = SelectedView.LineID;
                foreach (Column3D column in SelectedScene.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        if (view.LineID == lineID)
                        {
                            view.Default();
                        }
                    }
                }
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