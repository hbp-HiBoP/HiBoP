using HBP.Core.Data;
using HBP.Core.Tools;
using System;
using System.IO;
using UnityEngine;

namespace HBP.Data.Preferences
{
    public class PersistentDataManager : Singleton<PersistentDataManager>
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
            InitializePreferences();
            InitializeTags();
        }
        private void InitializePreferences()
        {
            if (new FileInfo(UserPreferences.PATH).Exists)
            {
                try
                {
                    m_UserPreferences = ClassLoaderSaver.LoadFromJson<UserPreferences>(UserPreferences.PATH);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    m_UserPreferences = new UserPreferences();
                }
            }
            else
            {
                m_UserPreferences = new UserPreferences();
            }
            m_UserPreferences.Save();
        }
        private void InitializeTags()
        {
            if (new FileInfo(TagCollection.PATH).Exists)
            {
                try
                {
                    m_Tags = ClassLoaderSaver.LoadFromJson<TagCollection>(TagCollection.PATH);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    m_Tags = new TagCollection();
                }
            }
            else
            {
                m_Tags = new TagCollection();
            }
            m_Tags.Save();
        }
        #endregion
    }
}