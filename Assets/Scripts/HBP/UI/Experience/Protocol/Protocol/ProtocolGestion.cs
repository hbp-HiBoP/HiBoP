using System.Linq;
using Tools.CSharp;
using d = HBP.Data.Experience.Protocol;
using UnityEngine.UI;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolGestion : ItemGestion<d.Protocol>
    {
        #region Properties
        Text m_protocolsCounter;
        #endregion

        #region Public Methods
        public override void Save()
		{
            ApplicationState.ProjectLoaded.SetProtocols(Items.ToArray());
            base.Save();
        }
        public override void Remove()
        {
            base.Remove();
            m_protocolsCounter.text = m_List.ObjectsSelected.Count().ToString();
        }
        public void Import()
        {
            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "prov" }, "Please select the protocols file to import");
            StringExtension.StandardizeToPath(ref l_resultStandalone);
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
            (m_List as ProtocolList).SortByName(ProtocolList.Sorting.Descending);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_List = transform.Find("Content").Find("Protocols").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<ProtocolList>();
            (m_List as ProtocolList).OnAction.AddListener((item, i) => OpenModifier(item, true));
            AddItem(ApplicationState.ProjectLoaded.Protocols.ToArray());

            m_protocolsCounter = transform.Find("Content").Find("Buttons").Find("ItemSelected").Find("Counter").GetComponent<Text>();
            m_List.OnSelectionChanged.AddListener((g, b) => m_protocolsCounter.text = m_List.ObjectsSelected.Count().ToString());
        }
		#endregion
	}
}
