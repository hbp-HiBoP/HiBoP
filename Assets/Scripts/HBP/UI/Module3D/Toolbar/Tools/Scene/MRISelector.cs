using HBP.Module3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class MRISelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the MRI to be selected
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the MRI
        /// </summary>
        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
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

                SelectedScene.MRIManager.Select(m_Dropdown.options[value].text);
                OnChangeValue.Invoke(value);
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
            foreach (Core.Object3D.MRI3D mri in SelectedScene.MRIManager.MRIs)
            {
                m_Dropdown.options.Add(new Dropdown.OptionData(mri.Name));
            }
            m_Dropdown.value = SelectedScene.MRIManager.SelectedMRIID;
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}