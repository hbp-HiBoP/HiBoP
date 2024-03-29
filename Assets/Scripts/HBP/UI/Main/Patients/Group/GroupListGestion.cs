﻿using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class GroupListGestion : ListGestion<Core.Data.Group>
    {
        #region Properties
        [SerializeField] protected GroupList m_List;
        public override ActionableList<Core.Data.Group> List => m_List;

        [SerializeField] protected GroupCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Group> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}