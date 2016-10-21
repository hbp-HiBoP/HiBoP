using UnityEngine;

public class PatientMenu : MonoBehaviour
{
    [SerializeField]
    GameObject patientGestionPrefab;

    [SerializeField]
    GameObject groupGestionPrefab;

    public void OpenPatientGestion()
    {
        RectTransform obj = Instantiate(patientGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.Patient.PatientGestion>().Open();
    }

    public void OpenGroupGestion()
    {
        RectTransform obj = Instantiate(groupGestionPrefab).GetComponent<RectTransform>();
        obj.SetParent(GameObject.Find("Windows").transform);
        obj.localPosition = new Vector3(0, 0, 0);
        obj.GetComponent<HBP.UI.Patient.GroupGestion>().Open();
    }
}
