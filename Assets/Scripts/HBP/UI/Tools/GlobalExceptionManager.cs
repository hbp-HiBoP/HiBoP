using HBP.Core.Tools;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class GlobalExceptionManager : Manager<GlobalExceptionManager>
    {
        private const int MAX_PENDING_EXCEPTIONS = 1024;
        private static readonly TimeSpan s_QuietPeriod = TimeSpan.FromSeconds(5);

        #region Private Methods

        private readonly ConcurrentQueue<PendingException> m_PendingExceptions = new();
        private readonly ExceptionIncidentTracker m_IncidentTracker = new(s_QuietPeriod);
        private int m_PendingExceptionCount;
        private bool m_OpenWindowPending;

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleException;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleException;
        }

        private void HandleException(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || Interlocked.Increment(ref m_PendingExceptionCount) > MAX_PENDING_EXCEPTIONS)
            {
                Interlocked.Decrement(ref m_PendingExceptionCount);
                return;
            }

            m_PendingExceptions.Enqueue(new PendingException(condition, stackTrace, DateTime.UtcNow));
        }

        private void Update()
        {
            while (m_PendingExceptions.TryDequeue(out PendingException exception))
            {
                Interlocked.Decrement(ref m_PendingExceptionCount);
                m_OpenWindowPending |= m_IncidentTracker.Add(exception.Condition, exception.StackTrace, exception.TimestampUtc);
            }

            if (m_OpenWindowPending && WindowsManager.IsInitialized)
            {
                WindowsManager.Open("Bug Reporter window", null);
                m_OpenWindowPending = false;
            }
        }

        private sealed class PendingException
        {
            public string Condition { get; }
            public string StackTrace { get; }
            public DateTime TimestampUtc { get; }

            public PendingException(string condition, string stackTrace, DateTime timestampUtc)
            {
                Condition = condition;
                StackTrace = stackTrace;
                TimestampUtc = timestampUtc;
            }
        }

        #endregion

        #region Public Methods

        internal static ExceptionIncidentSnapshot GetCurrentIncident()
        {
            return IsInitialized ? m_Instance.m_IncidentTracker.CreateSnapshot() : null;
        }

        internal static void CloseCurrentIncident()
        {
            if (IsInitialized)
            {
                m_Instance.m_IncidentTracker.CloseActiveIncident(DateTime.UtcNow);
            }
        }

        #endregion
    }
}
