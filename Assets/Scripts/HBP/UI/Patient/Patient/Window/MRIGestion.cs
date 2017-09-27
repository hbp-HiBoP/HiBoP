using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MRIGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] MRIList m_MRIList;
        [SerializeField] ItemModifier<MRI> m_Modifier;
        [SerializeField] GameObject m_ModifierPrefab;
        [SerializeField] Text m_MRICounter;
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
                m_MRIList.interactable = interactable;
            }
        }
        #endregion

        #region Public Methods
        public void Set(Data.Patient patient)
        {
            m_Patient = patient;
            m_MRIList.OnSelectionChanged.RemoveAllListeners();
            m_MRIList.OnSelectionChanged.AddListener((mesh, i) => m_MRICounter.text = m_MRIList.ObjectsSelected.Length.ToString());

            m_MRIList.OnAction.RemoveAllListeners();
            m_MRIList.OnAction.AddListener((mesh, i) => OpenModifier(mesh, interactable));

            m_MRIList.Objects = m_Patient.Brain.MRIs.ToArray();
            m_MRIList.SortByName(MRIList.Sorting.Descending);
        }
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            m_MRIList.SortByName(MRIList.Sorting.Descending);
        }
        public void AddMRI()
        {
            OpenModifier(new MRI(),interactable);
        }
        public void RemoveMRI()
        {
            m_MRIList.Remove(m_MRIList.ObjectsSelected);
            m_Patient.Brain.MRIs = m_MRIList.Objects.ToList();
            m_MRICounter.text = m_MRIList.ObjectsSelected.Count().ToString();
        }
        public void Save()
        {
            m_Patient.Brain.MRIs = m_MRIList.Objects.ToList();
        }
        #endregion

        #region Private Methods
        void OpenModifier(MRI MRI, bool interactable)
        {
            RectTransform obj = Instantiate(m_ModifierPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            m_Modifier = obj.GetComponent<ItemModifier<MRI>>();
            m_Modifier.Open(MRI, interactable);
            m_Modifier.CloseEvent.AddListener(() => OnCloseModifier());
            m_Modifier.SaveEvent.AddListener(() => OnSaveModifier());
        }
        void OnCloseModifier()
        {
            m_Modifier = null;
        }
        void OnSaveModifier()
        {
            if (!m_MRIList.Objects.Contains(m_Modifier.Item))
            {
                m_MRIList.Add(m_Modifier.Item);
            }
            else
            {
                m_MRIList.UpdateObject(m_Modifier.Item);
            }
            m_MRIList.SortByNone();
        }
        #endregion
    }
}