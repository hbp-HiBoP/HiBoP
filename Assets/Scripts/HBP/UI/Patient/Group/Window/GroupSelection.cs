using System.Linq;
using HBP.Data;
using UnityEngine.Events;

namespace HBP.UI.Anatomy
{
    public class GroupSelection : Window
    {
        #region Properties
        GroupList groupList;
        public GroupsSelected GroupsSelectedEvent = new GroupsSelected();
        #endregion

        #region Public Methods
        public void AddGroupsSelected()
        {
            GroupsSelectedEvent.Invoke(groupList.ObjectsSelected);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            groupList = transform.Find("Content").Find("Groups").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<GroupList>();
            groupList.Objects = ApplicationState.ProjectLoaded.Groups.ToArray();
            groupList.SortByName(GroupList.Sorting.Descending);
        }
        #endregion
    }

    public class GroupsSelected : UnityEvent<Group[]> { };
}
