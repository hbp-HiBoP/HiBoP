


/**
 * \file    ThreadedJob.cs
 * \author  Lance Florian
 * \date    01/01/2016
 * \brief   Define ThreadedJob
 */


// system
using System.Collections;

namespace HBP.VISU3D
{

    public class ThreadedJob
    {
        private bool m_IsDone = false;
        private object m_Handle = new object();
        private System.Threading.Thread m_Thread = null;
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

        public virtual void Start()
        {
            m_Thread = new System.Threading.Thread(Run);
            m_Thread.Start();
        }
        public virtual void Abort()
        {
            m_Thread.Abort();
        }

        protected virtual void ThreadFunction() { }

        protected virtual void OnFinished() { }

        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }

            return false;
        }

        IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }

        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
    }
}