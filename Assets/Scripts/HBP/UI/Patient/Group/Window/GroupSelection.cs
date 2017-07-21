using System.Linq;
using HBP.Data;
using UnityEngine.Events;

namespace HBP.UI.Anatomy
{
    public class GroupSelection : Window
    {
        #region Properties
        GroupList groupList;
        public AddGroupsEvent AddGroupsEvent = new AddGroupsEvent();
        #endregion

        #region Public Methods
        public void AddGroupsSelected()
        {
            AddGroupsEvent.Invoke(groupList.ObjectsSelected);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            groupList = transform.Find("Content").Find("Groups").Find("Scroll View").Find("Viewport").Find("Content").GetComponent<GroupList>();
            groupList.Objects = ApplicationState.ProjectLoaded.Groups.ToArray();
        }
        #endregion
    }

    public class AddGroupsEvent : UnityEvent<Group[]> { };
}
