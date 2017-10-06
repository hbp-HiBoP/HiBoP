using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Visualization
{
    public class BugReporterMenu : MonoBehaviour
    {
        public void OpenBugReporter()
        {
            FindObjectOfType<GlobalExceptionManager>().GetComponent<GlobalExceptionManager>().OpenBugReporter();
        }
    }
}

