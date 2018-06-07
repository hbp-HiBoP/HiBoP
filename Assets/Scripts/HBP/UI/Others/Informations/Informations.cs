using UnityEngine;
using UnityEngine.Events;
using Tools.Unity.ResizableGrid;
using HBP.Module3D;

namespace HBP.UI.Informations
{
    public class Informations : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        public Base3DScene Scene
        {
            get { return m_Scene; }
            set
            {
                m_Scene = value;
                SiteInformations.Scene = value;
                ROIInformations.Scene = value;
                //m_Scene.OnChangeColumnMinimizedState.AddListener(OnMinimizeColumns);
                //m_Scene.OnUpdateROI.AddListener(OnUpdateROI);
            }
        }

        public SiteInformations SiteInformations;
        public ROIInformations ROIInformations;

        public float MinimumWidth = 200.0f;
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ParentGrid.MinimumViewWidth) <= MinimumWidth;
            }
        }

        [HideInInspector] public UnityEvent OnOpenInformationsWindow = new UnityEvent();
        [HideInInspector] public UnityEvent OnCloseInformationsWindow = new UnityEvent();

        [SerializeField] RectTransform m_RectTransform;
        [SerializeField] GameObject m_MinimizedGameObject;
        ResizableGrid m_ParentGrid;
        #endregion

        #region Public Methods
        public void Open(bool open)
        {
            if(open)
            {
                OnOpenInformationsWindow.Invoke();
            }
            else
            {
                OnCloseInformationsWindow.Invoke();
            }
        }
        public void ChangeOverlayState(bool state)
        {
            transform.Find("Borders").Find("BotBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("LeftBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("RightBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("TopBorder").gameObject.SetActive(state);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_ParentGrid = GetComponentInParent<ResizableGrid>();
        }
        void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransform.hasChanged = false;
            }
        }
        #endregion
    }
}