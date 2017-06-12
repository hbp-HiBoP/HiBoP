using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationMenu : MonoBehaviour
    {
        [SerializeField]
        GameObject m_visualizationGestionPrefab;

        public void OpenVisualizationGestion()
        {
            Instantiate(m_visualizationGestionPrefab, GameObject.Find("Windows").transform, false).GetComponent<VisualizationGestion>().Open();
        }
    }
}

