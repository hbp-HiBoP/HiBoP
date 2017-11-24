using System.Linq;
using HBP.Data;
using UnityEngine.Events;
using UnityEngine;
using System.Collections.Generic;

namespace HBP.UI.Anatomy
{
    public class GroupSelection : Window
    {
        #region Properties
        [SerializeField] GameObject m_GroupModifierPrefab;
        List<GroupModifier> m_Modifiers = new List<GroupModifier>();
        GroupList groupList;
        public GroupsSelected GroupsSelectedEvent = new GroupsSelected();
        #endregion

        #region Public Methods
        public override void Close()
        {
            foreach (var modifier in m_Modifiers.ToArray()) modifier.Close();
            m_Modifiers.Clear();
            base.Close();
        }
        public void AddGroupsSelected()
        {
            GroupsSelectedEvent.Invoke(groupList.ObjectsSelected);
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            groupList = transform.Find("Content").Find("Groups").Find("List").Find("Display").GetComponent<GroupList>();
            groupList.Objects = ApplicationState.ProjectLoaded.Groups.ToArray();
            groupList.OnAction.AddListener((group, action) => OpenModifier(group));
            groupList.SortByName(GroupList.Sorting.Descending);
        }
        protected virtual void OpenModifier(Group item)
        {
            groupList.DeselectAll();
            RectTransform obj = Instantiate(m_GroupModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            GroupModifier modifier = obj.GetComponent<GroupModifier>();
            modifier.Open(item, false);
            modifier.CloseEvent.AddListener(() => OnCloseModifier(modifier));
            m_Modifiers.Add(modifier);
        }
        protected virtual void OnCloseModifier(GroupModifier modifier)
        {
            m_Modifiers.Remove(modifier);
        }
        #endregion
    }

    public class GroupsSelected : UnityEvent<Group[]> { };
}
