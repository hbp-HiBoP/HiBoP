using System.Linq;
using HBP.Data;

namespace HBP.UI.Patient
{
    public class GroupGestion : ItemGestion<Group>
    {
        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetGroups(Items.ToArray());
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void SetWindow()
        {
            list = transform.FindChild("Content").FindChild("List").FindChild("List").FindChild("GameObject").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<GroupList>();
            (list as GroupList).ActionEvent.AddListener((item,i) => OpenModifier(item,true));
            AddItem(ApplicationState.ProjectLoaded.Groups.ToArray());
        }
        #endregion
    }
}