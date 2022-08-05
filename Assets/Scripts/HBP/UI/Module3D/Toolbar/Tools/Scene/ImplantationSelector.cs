using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ImplantationSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the reference system to be used
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ImplantationManager.Select(m_Dropdown.options[value].text);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.value = 0;
            m_Dropdown.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedScene != null)
            {
                foreach (Core.Object3D.Implantation3D implantation in SelectedScene.ImplantationManager.Implantations)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(implantation.Name));
                }
                m_Dropdown.value = SelectedScene.ImplantationManager.SelectedImplantationID;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}