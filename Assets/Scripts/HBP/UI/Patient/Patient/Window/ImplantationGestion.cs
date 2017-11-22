using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class ImplantationGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] ImplantationList m_ImplantationList;
        [SerializeField] ItemModifier<Implantation> m_Modifier;
        [SerializeField] GameObject m_ModifierPrefab;
        [SerializeField] Text m_ImplantationCounter;
        [SerializeField] Button m_AddButton;
        [SerializeField] Button m_RemoveButton;
        Data.Patient m_Patient;
        bool m_Interactable;
        public virtual bool interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                m_AddButton.interactable = interactable;
                m_RemoveButton.interactable = interactable;
                m_ImplantationList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_ImplantationList.Initialize();

            m_ImplantationList.OnSelectionChanged.RemoveAllListeners();
            m_ImplantationList.OnSelectionChanged.AddListener((mesh, i) => m_ImplantationCounter.text = m_ImplantationList.ObjectsSelected.Length.ToString());

            m_ImplantationList.OnAction.RemoveAllListeners();
            m_ImplantationList.OnAction.AddListener((implantation, i) => OpenModifier(implantation, interactable));

            m_ImplantationList.Objects = m_Patient.Brain.Implantations.ToArray();
            m_ImplantationList.SortByName(ImplantationList.Sorting.Descending);
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            m_ImplantationList.SortByName(ImplantationList.Sorting.Descending);
        }
        public void AddImplantation()
        {
            OpenModifier(new Implantation(), interactable);
        }
        public void RemoveImplantation()
        {
            m_ImplantationList.Remove(m_ImplantationList.ObjectsSelected);
            m_Patient.Brain.Implantations = m_ImplantationList.Objects.ToList();
            m_ImplantationCounter.text = m_ImplantationList.ObjectsSelected.Count().ToString();
        }
        public void Save()
        {
            m_Patient.Brain.Implantations = m_ImplantationList.Objects.ToList();
        }
        #endregion

        #region Private Methods
        void OpenModifier(Implantation implantation, bool interactable)
        {
            RectTransform obj = Instantiate(m_ModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            m_Modifier = obj.GetComponent<ItemModifier<Implantation>>();
            m_Modifier.Open(implantation, interactable);
            m_Modifier.CloseEvent.AddListener(() => OnCloseModifier());
            m_Modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        void OnCloseModifier()
        {
            m_Modifier = null;
        }
        void OnSaveModifier()
        {
            if (!m_ImplantationList.Objects.Contains(m_Modifier.Item))
            {
                m_ImplantationList.Add(m_Modifier.Item);
            }
            else
            {
                m_ImplantationList.UpdateObject(m_Modifier.Item);
            }
            m_ImplantationList.SortByNone();
        }
        #endregion
    }
}