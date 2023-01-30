using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class CreatorContextMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField] bool m_IsLoadableFromFile = false;
        public bool IsCreatableFromFile
        {
            get
            {
                return m_IsLoadableFromFile;
            }
            set
            {
                m_IsLoadableFromFile = value;
                Set();
            }
        }

        [SerializeField] bool m_IsLoadableFromDatabase = false;
        public bool IsCreatableFromDatabase
        {
            get
            {
                return m_IsLoadableFromDatabase;
            }
            set
            {
                m_IsLoadableFromDatabase = value;
                Set();
            }
        }

        [SerializeField] bool m_IsLoadableFromDirectory = false;
        public bool IsCreatableFromDirectory
        {
            get
            {
                return m_IsLoadableFromDirectory;
            }
            set
            {
                m_IsLoadableFromDirectory = value;
                Set();
            }
        }

        [SerializeField] bool m_IsCreatableFromScratch = false;
        public bool IsCreatableFromScratch
        {
            get
            {
                return m_IsCreatableFromScratch;
            }
            set
            {
                m_IsCreatableFromScratch = value;
                Set();
            }
        }

        [SerializeField] bool m_IsCreatableFromExistingObjects = false;
        public bool IsCreatableFromExistingObjects
        {
            get
            {
                return m_IsCreatableFromExistingObjects;
            }
            set
            {
                m_IsCreatableFromExistingObjects = value;
                Set();
            }
        }
        
        [SerializeField] Button m_FromScratchButton;
        [SerializeField] Button m_FromExistingObjectButton;
        [SerializeField] Button m_FromFileButton;
        [SerializeField] Button m_FromDatabaseButton;
        [SerializeField] Button m_FromDirectoryButton;
        [SerializeField] Button m_Blocker;
        #endregion

        #region Events
        public GenericEvent<CreationType> OnSelectType = new GenericEvent<CreationType>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_FromScratchButton.onClick.AddListener(() => OnSelectType.Invoke(CreationType.FromScratch));
            m_FromExistingObjectButton.onClick.AddListener(() => OnSelectType.Invoke(CreationType.FromExistingObject));
            m_FromFileButton.onClick.AddListener(() => OnSelectType.Invoke(CreationType.FromFile));
            m_FromDatabaseButton.onClick.AddListener(() => OnSelectType.Invoke(CreationType.FromDatabase));
            m_FromDirectoryButton.onClick.AddListener(() => OnSelectType.Invoke(CreationType.FromDirectory));
        }
        private void Set()
        {
            m_FromScratchButton.gameObject.SetActive(IsCreatableFromScratch);
            m_FromExistingObjectButton.gameObject.SetActive(IsCreatableFromExistingObjects);
            m_FromFileButton.gameObject.SetActive(IsCreatableFromFile);
            m_FromDatabaseButton.gameObject.SetActive(IsCreatableFromDatabase);
            m_FromDirectoryButton.gameObject.SetActive(IsCreatableFromDirectory);
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            Set();
            gameObject.SetActive(true);
        }
        public void Close()
        {
            gameObject.SetActive(false);
        }
        #endregion
    }
}