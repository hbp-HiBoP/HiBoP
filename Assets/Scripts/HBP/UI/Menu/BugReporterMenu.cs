using Tools.Unity;
using UnityEngine;

namespace HBP.UI
{
    public class BugReporterMenu : MonoBehaviour
    {
        public void OpenBugReporter()
        {
            FindObjectOfType<GlobalExceptionManager>().GetComponent<GlobalExceptionManager>().OpenBugReporter();
        }
    }
}

