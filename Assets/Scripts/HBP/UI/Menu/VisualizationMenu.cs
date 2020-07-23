using UnityEngine;

namespace HBP.UI
{
    public class VisualizationMenu : Menu
    {
        #region Public Methods
        public void OpenVisualizationGestion()
        {
            ApplicationState.WindowsManager.Open("Visualization gestion window");
        }
        #endregion
    }
}

