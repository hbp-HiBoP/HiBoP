using UnityEngine;

namespace HBP.UI.Visualization
{
    public class VisualizationMenu : MonoBehaviour
    {
        [SerializeField]
        GameObject singlePatientVisualizationGestionPrefab;

        [SerializeField]
        GameObject multiPatientsVisualizationGestionPrefab;

        public void OpenSinglePatientVisualizationGestion()
        {
            RectTransform obj = Instantiate(singlePatientVisualizationGestionPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<SingleVisualizationGestion>().Open();
        }

        public void OpenMultiPatientsVisualizationGestion()
        {
            RectTransform obj = Instantiate(multiPatientsVisualizationGestionPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<MultiVisualizationGestion>().Open();
        }
    }
}

