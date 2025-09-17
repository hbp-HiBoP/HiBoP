using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Object3D;

namespace HBP.UI.Toolbar
{
    public class LocalizersSelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the protocol to display
        /// </summary>
        [SerializeField] private Dropdown m_ProtocolDropdown;
        /// <summary>
        /// Dropdown to select the data to display
        /// </summary>
        [SerializeField] private Dropdown m_DataDropdown;
        /// <summary>
        /// Dropdown to select the bloc to display
        /// </summary>
        [SerializeField] private Dropdown m_BlocDropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_ProtocolDropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                if (value >= 0 && value < m_ProtocolDropdown.options.Count)
                {
                    SelectedScene.FMRIManager.SelectedLocalizersProtocol = m_ProtocolDropdown.options[value].text;
                }
            });
            m_DataDropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                if (value >= 0 && value < m_DataDropdown.options.Count)
                {
                    SelectedScene.FMRIManager.SelectedLocalizersData = m_DataDropdown.options[value].text;
                }
            });
            m_BlocDropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                if (value >= 0 && value < m_BlocDropdown.options.Count)
                {
                    SelectedScene.FMRIManager.SelectedLocalizersBloc = m_BlocDropdown.options[value].text;
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isLocalizersDisplayed = SelectedScene.FMRIManager.DisplayLocalizers;

            gameObject.SetActive(isLocalizersDisplayed);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            UpdateProtocolDropdown();
            UpdateDataDropdown();
            UpdateBlocDropdown();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Update the protocol dropdown with available protocols
        /// </summary>
        private void UpdateProtocolDropdown()
        {
            m_ProtocolDropdown.options.Clear();
            
            if (Object3DManager.Localizers.Loaded)
            {
                int selectedIndex = 0;
                int count = 0;
                
                foreach (var protocol in Object3DManager.Localizers.Protocols.OrderBy(p => p.Name))
                {
                    m_ProtocolDropdown.options.Add(new Dropdown.OptionData(protocol.Name));
                    if (protocol.Name == SelectedScene.FMRIManager.SelectedLocalizersProtocol)
                        selectedIndex = count;
                    count++;
                }
                
                m_ProtocolDropdown.value = selectedIndex;
            }
            
            m_ProtocolDropdown.RefreshShownValue();
        }
        /// <summary>
        /// Update the data dropdown with available datas from selected protocol
        /// </summary>
        private void UpdateDataDropdown()
        {
            m_DataDropdown.options.Clear();
            
            if (Object3DManager.Localizers.Loaded)
            {
                var selectedProtocol = Object3DManager.Localizers.Protocols
                    .FirstOrDefault(p => p.Name == SelectedScene.FMRIManager.SelectedLocalizersProtocol);
                
                if (selectedProtocol != null)
                {
                    int selectedIndex = 0;
                    int count = 0;
                    
                    foreach (var data in selectedProtocol.Datas.OrderBy(d => d.Name))
                    {
                        m_DataDropdown.options.Add(new Dropdown.OptionData(data.Name));
                        if (data.Name == SelectedScene.FMRIManager.SelectedLocalizersData)
                            selectedIndex = count;
                        count++;
                    }
                    
                    m_DataDropdown.value = selectedIndex;
                }
            }
            
            m_DataDropdown.RefreshShownValue();
        }
        /// <summary>
        /// Update the bloc dropdown with available blocs from selected data
        /// </summary>
        private void UpdateBlocDropdown()
        {
            m_BlocDropdown.options.Clear();
            
            if (Object3DManager.Localizers.Loaded)
            {
                var selectedProtocol = Object3DManager.Localizers.Protocols
                    .FirstOrDefault(p => p.Name == SelectedScene.FMRIManager.SelectedLocalizersProtocol);
                var selectedData = selectedProtocol?.Datas
                    .FirstOrDefault(d => d.Name == SelectedScene.FMRIManager.SelectedLocalizersData);
                
                if (selectedData != null)
                {
                    int selectedIndex = 0;
                    int count = 0;
                    
                    foreach (var bloc in selectedData.Blocs.OrderBy(b => b.Name))
                    {
                        m_BlocDropdown.options.Add(new Dropdown.OptionData(bloc.Name));
                        if (bloc.Name == SelectedScene.FMRIManager.SelectedLocalizersBloc)
                            selectedIndex = count;
                        count++;
                    }
                    
                    m_BlocDropdown.value = selectedIndex;
                }
            }
            
            m_BlocDropdown.RefreshShownValue();
        }
        #endregion
    }
}