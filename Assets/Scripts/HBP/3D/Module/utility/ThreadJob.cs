


/**
 * \file    ThreadedJob.cs
 * \author  Lance Florian
 * \date    01/01/2016
 * \brief   Define ThreadedJob
 */


// system
using System.Collections;

namespace HBP.Module3D
{
    public class ThreadedJob
    {
        #region Properties
        private bool m_IsDone = false;
        private object m_Handle = new object();
        private System.Threading.Thread m_Thread = null;
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        protected virtual void ThreadFunction() { }
        /// <summary>
        /// 
        /// </summary>
        protected virtual void OnFinished() { }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (m_Handle)
                {
                    tmp = m_IsDone;
                }
                return tmp;
            }
            set
            {
                lock (m_Handle)
                {
                    m_IsDone = value;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Start()
        {
            m_Thread = new System.Threading.Thread(Run);
            m_Thread.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        public virtual void Abort()
        {
            m_Thread.Abort();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }

            return false;
        }
        #endregion
    }
}