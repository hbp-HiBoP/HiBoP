using System.Linq;
using Tools.CSharp;
using d = HBP.Data.Experience.Protocol;
using UnityEngine.UI;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolGestion : ItemGestion<d.Protocol>
    {
        #region Properties
        [SerializeField] Text m_ProtocolsCounter;
        [SerializeField] ProtocolList m_ProtocolList;
        #endregion

        #region Public Methods
        public override void Save()
		{
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetProtocols(Items.ToArray());
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetProtocols(Items.ToArray());
                base.Save();
            }
        }
        public override void Remove()
        {
            base.Remove();
            m_ProtocolsCounter.text = m_List.ObjectsSelected.Count().ToString();
        }
        public void Import()
        {
            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "prov" }, "Please select the protocols file to import");
            l_resultStandalone = l_resultStandalone.StandardizeToPath();
            if (l_resultStandalone != string.Empty)
            {
                d.Protocol protocol = Tools.Unity.ClassLoaderSaver.LoadFromJson<d.Protocol>(l_resultStandalone);
                if (protocol.ID == "xxxxxxxxxxxxxxxxxxxxxxxxx" || Items.Any(p => p.ID == protocol.ID))
                {
                    protocol.ID = System.Guid.NewGuid().ToString();
                }
                AddItem(protocol);
            }
        }
        public override void Open()
        {
            base.Open();
            m_ProtocolList.SortByName(ProtocolList.Sorting.Descending);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_List = m_ProtocolList;
            m_ProtocolList.OnAction.AddListener((item, i) => OpenModifier(item, true));
            m_List.OnSelectionChanged.AddListener((g, b) => m_ProtocolsCounter.text = m_List.ObjectsSelected.Count().ToString());
            AddItem(ApplicationState.ProjectLoaded.Protocols.ToArray());
        }
        #endregion
    }
}
