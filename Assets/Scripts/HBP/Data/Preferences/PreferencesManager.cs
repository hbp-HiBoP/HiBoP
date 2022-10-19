using HBP.Core.Tools;
using System;
using System.IO;
using UnityEngine;

namespace HBP.Data.Preferences
{
    public class PreferencesManager : MonoBehaviour
    {
        #region Properties
        private static PreferencesManager m_Instance;

        private UserPreferences m_UserPreferences;
        public static UserPreferences UserPreferences { get { return m_Instance.m_UserPreferences; } }
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }

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
            ClassLoaderSaver.SaveToJSon(m_UserPreferences, UserPreferences.PATH, true);
        }
        #endregion
    }
}