using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.CSharp;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolGestion : SavableWindow
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ProtocolListGestion;
        [SerializeField] Button m_ImportButton;
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
                m_ImportButton.interactable = value;
                m_CreateProtocolButton.interactable = value;
                m_RemoveProtocolButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
		{
            foreach (var modifier in m_ProtocolListGestion.Modifiers) modifier.Save();
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetProtocols(m_ProtocolListGestion.Items);
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetProtocols((m_ProtocolListGestion.Items));
                base.Save();
            }
        }
        public void Import()
        {
            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "prov" }, "Please select the protocols file to import");
            StringExtension.StandardizeToPath(ref l_resultStandalone);
            if (l_resultStandalone != string.Empty)
            {
                d.Protocol protocol = Tools.Unity.ClassLoaderSaver.LoadFromJson<d.Protocol>(l_resultStandalone);
                if (protocol.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx" || m_ProtocolListGestion.Items.Any(p => p.ID == protocol.ID))
                {
                    protocol.ID = System.Guid.NewGuid().ToString();
                }
                m_ProtocolListGestion.Add(protocol);
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            m_ProtocolListGestion.Initialize(m_SubWindows);
            m_ProtocolListGestion.Items = ApplicationState.ProjectLoaded.Protocols.ToList();
            base.SetFields();
        }
        #endregion
    }
}