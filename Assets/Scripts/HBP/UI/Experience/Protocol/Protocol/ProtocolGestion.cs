using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolGestion : SavableWindow
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ProtocolListGestion;
        [SerializeField] Button m_CreateProtocolButton;
        [SerializeField] Button m_RemoveProtocolButton;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set 
            {
                base.Interactable = value;

                m_ProtocolListGestion.Interactable = value;
                m_CreateProtocolButton.interactable = value;
                m_RemoveProtocolButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
		{
            foreach (var modifier in m_ProtocolListGestion.SubWindows.ToArray()) modifier.Save();
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetProtocols(m_ProtocolListGestion.Objects);
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetProtocols((m_ProtocolListGestion.Objects));
                base.Save();
            }
            FindObjectOfType<MenuButtonState>().SetInteractables();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckDatasets(onChangeProgress, m_ProtocolListGestion.ModifiedProtocols), onChangeProgress);
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            m_ProtocolListGestion.Initialize(m_SubWindows);
            m_ProtocolListGestion.Objects = ApplicationState.ProjectLoaded.Protocols.ToList();
            base.SetFields();
        }
        #endregion
    }
}