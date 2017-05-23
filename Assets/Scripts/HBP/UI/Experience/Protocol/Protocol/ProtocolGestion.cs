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
            string l_resultStandalone = HBP.Module3D.DLL.QtGUI.GetExistingFileName(new string[] { "prov" }, "Please select the protocols file to import");
            StringExtension.StandardizeToPath(ref l_resultStandalone);
            if (l_resultStandalone != string.Empty)
            {
                d.Protocol protocol = Tools.Unity.ClassLoaderSaver.LoadFromJson<d.Protocol>(l_resultStandalone);
                AddItem(protocol);
            }
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            list = transform.Find("Content").Find("Protocols").Find("List").Find("Viewport").Find("Content").GetComponent<ProtocolList>();
            (list as ProtocolList).ActionEvent.AddListener((item, i) => OpenModifier(item, true));
            AddItem(ApplicationState.ProjectLoaded.Protocols.ToArray());
        }
		#endregion
	}
}
