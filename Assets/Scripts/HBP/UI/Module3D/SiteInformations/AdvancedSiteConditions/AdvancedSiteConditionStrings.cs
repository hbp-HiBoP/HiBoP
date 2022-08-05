using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public static class AdvancedSiteConditionStrings
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "AdvancedConditions.txt");
        public static string SEPARATOR = "//.//";
        private static List<string> m_Conditions = new List<string>();
        public static ReadOnlyCollection<string> Conditions { get { return new ReadOnlyCollection<string>(m_Conditions); } }
        #endregion

        #region Events
        public static UnityEvent OnChangeConditions = new UnityEvent();
        #endregion

        #region Public Methods
        public static void LoadConditions()
        {
            if (new FileInfo(PATH).Exists)
            {
                using (StreamReader sr = new StreamReader(PATH))
                {
                    m_Conditions.Clear();
                    string file = sr.ReadToEnd();
                    string[] conditions = file.Split(new string[] { SEPARATOR }, System.StringSplitOptions.RemoveEmptyEntries);
                    m_Conditions.AddRange(conditions);
                }
            }
        }
        public static void AddCondition(string condition)
        {
            if (!m_Conditions.Contains(condition))
            {
                m_Conditions.Add(condition);
                SaveConditions();
            }
        }
        public static void RemoveCondition(string condition)
        {
            if (m_Conditions.Contains(condition))
            {
                m_Conditions.Remove(condition);
                SaveConditions();
            }
        }
        #endregion

        #region Private Methods
        private static void SaveConditions()
        {
            using (StreamWriter sw = new StreamWriter(PATH))
            {
                foreach (var condition in m_Conditions)
                {
                    sw.Write(condition);
                    sw.Write(SEPARATOR);
                }
                OnChangeConditions.Invoke();
            }
        }
        #endregion
    }
}