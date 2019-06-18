using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class EdfDataContainerGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileSelector m_FileSelector;

        bool m_interactable;
        public bool interactable
        {
            get { return m_interactable; }
            set
            {
                m_interactable = value;
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(Data.Experience.Dataset.EdfDataContainer dataContainer)
        {
            m_FileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_FileSelector.File = dataContainer.SavedEDF;
            m_FileSelector.onValueChanged.RemoveAllListeners();
            m_FileSelector.onValueChanged.AddListener((edf) =>
            {
                dataContainer.EDF = edf;
            });
        }
        #endregion
    }
}