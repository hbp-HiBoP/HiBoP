namespace HBP.UI
{
    public class BugReporterMenu : Menu
    {
        public void OpenBugReporter()
        {
            ApplicationState.WindowsManager.Open("Bug Reporter window");
        }
    }
}

