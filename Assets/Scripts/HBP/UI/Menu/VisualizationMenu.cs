using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationMenu : MonoBehaviour
    {
        public void OpenVisualizationGestion()
        {
            ApplicationState.WindowsManager.Open("Visualization Gestion Window");
        }
    }
}

