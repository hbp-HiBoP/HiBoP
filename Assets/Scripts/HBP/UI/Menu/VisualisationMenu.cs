using UnityEngine;
using System.Collections;

public class VisualisationMenu : MonoBehaviour
{
    [SerializeField]
    GameObject singlePatientVisualisationGestionPrefab;

    [SerializeField]
    GameObject multiPatientsVisualisationGestionPrefab;

    public void OpenSinglePatientVisualisationGestion()
    {
        RectTransform obj = Instantiate(singlePatientVisualisationGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.SingleVisualisationGestion>().Open();
    }

    public void OpenMultiPatientsVisualisationGestion()
    {
        RectTransform obj = Instantiate(multiPatientsVisualisationGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.MultiVisualisationGestion>().Open();
    }
}
