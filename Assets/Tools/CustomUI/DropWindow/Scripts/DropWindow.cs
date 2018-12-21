using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class DropWindow : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Window to be displayed
        /// </summary>
        public GameObject Window;
        /// <summary>
        /// Blocker to hide the window when clicking elsewhere
        /// </summary>
        public GameObject BlockerPrefab;
        /// <summary>
        /// Instantiated blocker
        /// </summary>
        private GameObject m_Blocker;
        #endregion

        #region Events
        public BoolEvent OnChangeWindowState;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Window.SetActive(false);
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            m_Blocker = Instantiate(BlockerPrefab, GetTopmostCanvas(GetComponent<RectTransform>()).GetComponent<RectTransform>());
            m_Blocker.GetComponent<Button>().onClick.AddListener(() =>
            {
                ChangeWindowState();
            });
            Window.SetActive(true);
            OnChangeWindowState.Invoke(true);
        }
        public void Close()
        {
            Destroy(m_Blocker);
            Window.SetActive(false);
            OnChangeWindowState.Invoke(false);
        }
        /// <summary>
        /// Show / Hide the window
        /// </summary>
        public void ChangeWindowState()
        {
            if(Window.activeSelf) Close();
            else Open();
        }
        /// <summary>
        /// Get the topmost canvas of a component
        /// </summary>
        /// <param name="component">Component in a canvas</param>
        /// <returns>Canvas associated to the component</returns>
        public Canvas GetTopmostCanvas(Component component)
        {
            Canvas[] parentCanvases = component.GetComponentsInParent<Canvas>();
            if (parentCanvases != null && parentCanvases.Length > 0)
            {
                return parentCanvases[parentCanvases.Length - 1];
            }
            return null;
        }
        #endregion
    }
}