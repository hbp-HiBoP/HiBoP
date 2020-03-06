namespace HBP.UI
{
    public class BugReporterMenu : Menu
    {
        public void OpenBugReporter()
        {
            ApplicationState.GlobalExceptionManager.OpenBugReporter();
        }
    }
}

