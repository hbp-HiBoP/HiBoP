using UnityEngine;

public class ExperienceMenu : MonoBehaviour
{
    [SerializeField]
    GameObject protocolGestionPrefab;

    [SerializeField]
    GameObject datasetGestionPrefab;

    public void OpenProtocolGestion()
    {
        RectTransform obj = Instantiate(protocolGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.Experience.Protocol.ProtocolGestion>().Open();
    }

    public void OpenDatasetGestion()
    {
        RectTransform obj = Instantiate(datasetGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.Experience.Dataset.DatasetGestion>().Open();
    }
}
