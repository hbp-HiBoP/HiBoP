using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace HBP.Tests.PlayMode.Utilities
{
    public static class AsyncPlayModeTestUtilities
    {
        public static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException("Timed out while waiting for PlayMode condition.");
                }

                yield return null;
            }
        }

        public static async Task<Exception> CaptureExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
