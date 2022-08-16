﻿using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Component to display tag in list.
    /// </summary>
    public class TagItem : ActionnableItem<Core.Data.BaseTag>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_TypeText;

        public override Core.Data.BaseTag Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_TypeText.text = value.GetType().GetDisplayName();
            }
        }
        #endregion
    }
}