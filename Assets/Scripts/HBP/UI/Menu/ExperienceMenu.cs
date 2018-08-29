using UnityEngine;

namespace HBP.UI.Experience
{
    public class ExperienceMenu : MonoBehaviour
    {
        public void OpenProtocolGestion()
        {
            ApplicationState.WindowsManager.Open("Protocol Gestion Window", true);
        }

        public void OpenDatasetGestion()
        {
            ApplicationState.WindowsManager.Open("Dataset Gestion Window", true);
        }
    }
}