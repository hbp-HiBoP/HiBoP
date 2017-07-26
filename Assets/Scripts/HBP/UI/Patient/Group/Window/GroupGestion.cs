using System.Linq;
using HBP.Data;

namespace HBP.UI.Anatomy
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
            m_List = transform.Find("Content").Find("Groups").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<GroupList>();
            (m_List as GroupList).OnAction.AddListener((item,i) => OpenModifier(item,true));
            AddItem(ApplicationState.ProjectLoaded.Groups.ToArray());
        }
        #endregion
    }
}