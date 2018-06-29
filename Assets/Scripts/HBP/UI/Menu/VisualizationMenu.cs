using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationMenu : MonoBehaviour
    {
        [SerializeField]
        GameObject m_visualizationGestionPrefab;

        public void OpenVisualizationGestion()
        {
            VisualizationGestion.Open(true);
        }
    }
}

