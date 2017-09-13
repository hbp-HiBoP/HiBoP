using System.Linq;
using HBP.Data;
using UnityEngine.UI;

namespace HBP.UI.Anatomy
{
    public class GroupGestion : ItemGestion<Group>
    {
        #region Properties
        Text m_groupCounter;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetGroups(Items.ToArray());
            base.Save();
        }
        public override void Remove()
        {
            base.Remove();
            m_groupCounter.text = m_List.ObjectsSelected.Count().ToString();
        }
        #endregion

        #region Protected Methods
        protected override void SetWindow()
        {
            m_List = transform.Find("Content").Find("Groups").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<GroupList>();
            (m_List as GroupList).OnAction.AddListener((item,i) => OpenModifier(item,true));
            AddItem(ApplicationState.ProjectLoaded.Groups.ToArray());

            m_groupCounter = transform.Find("Content").Find("Buttons").Find("ItemSelected").Find("Counter").GetComponent<Text>();
            m_List.OnSelectionChanged.AddListener((g,b) => m_groupCounter.text = m_List.ObjectsSelected.Count().ToString());
        }
        #endregion
    }
}