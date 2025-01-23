using System;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace HBP.Core.Tools
{
    public class AsyncMethod
    {
        #region Properties
        private readonly Func<Action<float, float, LoadingText>, Task> m_TaskToExecute;
        #endregion

        #region Events
        public GenericEvent<float, float, LoadingText> OnUpdateProgress = new();
        #endregion

        #region Constructors
        public AsyncMethod(Func<Action<float, float, LoadingText>, Task> taskToExecute)
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
        private void UpdateProgressAsync(float progress, float duration, LoadingText message)
        {
            OnUpdateProgress.Invoke(progress, duration, message);
        }
        #endregion
    }
}