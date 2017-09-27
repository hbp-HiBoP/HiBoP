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
        [SerializeField] ItemModifier<Connectivity> m_Modifier;
        [SerializeField] GameObject m_ModifierPrefab;
        [SerializeField] Text m_ConnectivityCounter;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;
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
                m_AddButton.interactable = interactable;
                m_RemoveButton.interactable = interactable;
                m_ConnectivityList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_ConnectivityList.OnSelectionChanged.RemoveAllListeners();
            m_ConnectivityList.OnSelectionChanged.AddListener((mesh, i) => m_ConnectivityCounter.text = m_ConnectivityList.ObjectsSelected.Length.ToString());

            m_ConnectivityList.OnAction.RemoveAllListeners();
            m_ConnectivityList.OnAction.AddListener((implantation, i) => OpenModifier(implantation, interactable));

            m_ConnectivityList.Objects = m_Patient.Brain.Connectivities.ToArray();
            m_ConnectivityList.SortByName(ConnectivityList.Sorting.Descending);
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            m_ConnectivityList.SortByName(ConnectivityList.Sorting.Descending);
        }
        public void AddConnectivity()
        {
            OpenModifier(new Connectivity(), interactable);
        }
        public void RemoveConnectivity()
        {
            m_ConnectivityList.Remove(m_ConnectivityList.ObjectsSelected);
            m_Patient.Brain.Connectivities = m_ConnectivityList.Objects.ToList();
            m_ConnectivityCounter.text = m_ConnectivityList.ObjectsSelected.Count().ToString();
        }
        public void Save()
        {
            m_Patient.Brain.Connectivities = m_ConnectivityList.Objects.ToList();
        }
        #endregion

        #region Private Methods
        void OpenModifier(Connectivity connectivity, bool interactable)
        {
            RectTransform obj = Instantiate(m_ModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            m_Modifier = obj.GetComponent<ItemModifier<Connectivity>>();
            m_Modifier.Open(connectivity, interactable);
            m_Modifier.CloseEvent.AddListener(() => OnCloseModifier());
            m_Modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        void OnCloseModifier()
        {
            m_Modifier = null;
        }
        void OnSaveModifier()
        {
            if (!m_ConnectivityList.Objects.Contains(m_Modifier.Item))
            {
                m_ConnectivityList.Add(m_Modifier.Item);
            }
            else
            {
                m_ConnectivityList.UpdateObject(m_Modifier.Item);
            }
            m_ConnectivityList.SortByNone();
        }
        #endregion
    }
}