using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteListDisplayer : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private DropWindow m_DropWindow;
        [SerializeField]
        private SiteList m_List;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_List.Initialize();
            m_Button.onClick.AddListener(() =>
            {
                m_List.ObjectsList = ApplicationState.Module3D.SelectedColumn.Sites;
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }
        #endregion
    }
}