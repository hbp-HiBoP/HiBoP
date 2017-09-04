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
        [SerializeField] Text m_ConnectivityCounter;
        [SerializeField] Button m_AddConnectivity;
        [SerializeField] Button m_RemoveConnectivity;
        Data.Patient m_Patient;
        bool m_Interactable;
        public bool interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_AddConnectivity.interactable = interactable;
                m_RemoveConnectivity.interactable = interactable;
                m_ConnectivityList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Save()
        {
            m_ConnectivityList.SaveAll();
        }
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_ConnectivityList.OnSelectionChanged.RemoveAllListeners();
            m_ConnectivityList.OnSelectionChanged.AddListener((mesh, i) => m_ConnectivityCounter.text = m_ConnectivityList.ObjectsSelected.Length.ToString());
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active) m_ConnectivityList.Objects = m_Patient.Brain.Connectivities.ToArray();
        }
        public void AddConnectivity()
        {
            Connectivity connectivity = new Connectivity();
            m_ConnectivityList.Add(connectivity);
            m_Patient.Brain.Connectivities.Add(connectivity);
        }
        public void RemoveConnectivity()
        {
            m_ConnectivityList.Remove(m_ConnectivityList.ObjectsSelected);
            m_Patient.Brain.Connectivities = m_ConnectivityList.Objects.ToList();
        }
        #endregion
    }
}