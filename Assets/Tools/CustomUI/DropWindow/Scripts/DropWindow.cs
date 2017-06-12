using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary>
    /// Content of the DropWindow
    /// </summary>
    public RectTransform Content;
    #endregion

    #region Private Methods
    private void Awake()
    {
        Window.SetActive(false);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Show / Hide the window
    /// </summary>
    public void ChangeWindowState()
    {
        if (!Window.activeSelf)
        {
            m_Blocker = Instantiate(BlockerPrefab, GetTopmostCanvas(GetComponent<RectTransform>()).GetComponent<RectTransform>());
            m_Blocker.GetComponent<Button>().onClick.AddListener(() =>
            {
                ChangeWindowState();
            });
        }
        else
        {
            Destroy(m_Blocker);
        }
        Window.SetActive(!Window.activeSelf);
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
