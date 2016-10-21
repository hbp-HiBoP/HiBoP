using System.Linq;
using Tools.CSharp;
using d = HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class ProtocolGestion : ItemGestion<d.Protocol>
    {
		#region Public Methods
		public override void Save()
		{
            ApplicationState.ProjectLoaded.SetProtocols(Items.ToArray());
            base.Save();
        }
        public void Import()
        {
            string l_resultStandalone = VISU3D.DLL.QtGUI.getOpenFileName(new string[] { "prov" }, "Please select the protocols file to import");
            l_resultStandalone.StandardizeToPath();
            if (l_resultStandalone != string.Empty)
            {
                AddItem(d.Protocol.LoadJSon(l_resultStandalone));
            }
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            list = transform.FindChild("Content").FindChild("Protocols").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<ProtocolList>();
            (list as ProtocolList).ActionEvent.AddListener((item, i) => OpenModifier(item, true));
            AddItem(ApplicationState.ProjectLoaded.Protocols.ToArray());
        }
		#endregion
	}
}
