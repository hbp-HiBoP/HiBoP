using System;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace HBP.Core.Tools
{
    public class AsyncMethod
    {
        #region Properties
        private readonly Func<Func<float, string, Task>, Task> m_TaskToExecute;

        private float m_Progress;
        public float Progress => m_Progress;

        private string m_Message;
        public string Message => m_Message;
        #endregion

        #region Events
        public GenericEvent<float, string> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public AsyncMethod(Func<Func<float, string, Task>, Task> taskToExecute)
        {
            m_TaskToExecute = taskToExecute;
        }
        #endregion

        #region Public Methods
        public async Task ExecuteAsync()
        {
            await m_TaskToExecute(UpdateProgressAsync);
        }
        #endregion

        #region Private Methods
        private async Task UpdateProgressAsync(float progress, string message)
        {
            m_Progress = progress;
            m_Message = message;
            OnUpdateProgress.Invoke(m_Progress, m_Message);
            await Task.Yield();
        }
        #endregion
    }
}