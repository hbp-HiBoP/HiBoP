using UnityEngine;
using System.Linq;
using System.Collections;
using HBP.Data.Patient;

namespace HBP.UI.Patient
{
    public class GroupSelection : Window
    {
        [SerializeField]
        GroupList m_groupList;

        #region Public Methods
        public Group[] GetGroupSelected()
        {
            return m_groupList.GetObjectsSelected();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            m_groupList.Display(ApplicationState.ProjectLoaded.Groups.ToArray());

            // TODO : Voir si il faut ajouter un nouveau panel pour voir ce qu'il y a dans un group.
            //m_groupList.ActionEvent.RemoveAllListeners();
            //m_groupList.ActionEvent.AddListener((group, i) => OpenGroupModifier(group));
        }
        #endregion
    }
}
