using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using System.IO;
using UnityEngine;

namespace HBP.Data.Preferences
{
    public class PersistentDataManager : Manager<PersistentDataManager>
    {
        #region Properties
        private UserPreferences m_UserPreferences;
        public static UserPreferences UserPreferences { get { return m_Instance.m_UserPreferences; } }

        private TagCollection m_Tags;
        public static TagCollection Tags { get { return m_Instance.m_Tags; } }
        #endregion

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_UserPreferences = UserPreferences.Initialize();
            m_Tags = TagCollection.Initialize();
        }
        #endregion
    }
}