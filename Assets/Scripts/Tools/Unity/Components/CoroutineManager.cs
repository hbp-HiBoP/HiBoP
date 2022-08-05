using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThirdParty.CielaSpike;

namespace HBP.Core.Tools
{
    public class CoroutineManager : MonoBehaviour
    {
        #region Properties
        private static CoroutineManager m_Instance;
        private List<Coroutine> m_Coroutines = new List<Coroutine>();
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
        }
        #endregion

        #region Public Methods
        public static Coroutine StartSync(IEnumerator coroutine)
        {
            if (m_Instance == null) throw new System.Exception("The Coroutine Manager is not present in the scene");

            Coroutine l_coroutine = m_Instance.StartCoroutine(coroutine);
            m_Instance.m_Coroutines.Add(l_coroutine);
            return l_coroutine;
        }
        public static Coroutine StartAsync(IEnumerator coroutine)
        {
            if (m_Instance == null) throw new System.Exception("The Coroutine Manager is not present in the scene");

            Coroutine l_coroutine = m_Instance.StartCoroutineAsync(coroutine);
            m_Instance.m_Coroutines.Add(l_coroutine);
            return l_coroutine;
        }
        public static void Stop(Coroutine coroutine)
        {
            if (m_Instance == null) throw new System.Exception("The Coroutine Manager is not present in the scene");

            m_Instance.m_Coroutines.Remove(coroutine);
            m_Instance.StopCoroutine(coroutine);
        }
        #endregion
    }
}