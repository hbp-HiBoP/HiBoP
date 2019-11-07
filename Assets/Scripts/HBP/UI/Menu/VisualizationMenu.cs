using UnityEngine;

namespace HBP.UI
{
    public class VisualizationMenu : MonoBehaviour
    {
        public void OpenVisualizationGestion()
        {
            ApplicationState.WindowsManager.Open("Visualization gestion window");
        }
    }
}

