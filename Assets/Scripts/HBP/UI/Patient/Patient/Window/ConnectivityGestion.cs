using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class ConnectivityGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] ConnectivityList m_ConnectivityList;
        [SerializeField] Button m_AddConnectivity;
        [SerializeField] Button m_RemoveConnectivity;
        Data.Patient m_Patient;
        #endregion

        #region Public Methods
        public void Save()
        {
            m_ConnectivityList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
        }
        public void SetInteractable(bool interactable)
        {
            m_AddConnectivity.interactable = interactable;
            m_RemoveConnectivity.interactable = interactable;
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active) m_ConnectivityList.Display(m_Patient.Brain.Connectivities.ToArray());
        }
        public void AddConnectivity()
        {
            Connectivity connectivity = new Connectivity();
            m_ConnectivityList.Add(connectivity);
            m_Patient.Brain.Connectivities.Add(connectivity);
        }
        public void RemoveConnectivity()
        {
            m_ConnectivityList.Remove(m_ConnectivityList.GetObjectsSelected());
            m_Patient.Brain.Connectivities = m_ConnectivityList.Objects.ToList();
        }
        #endregion
    }
}