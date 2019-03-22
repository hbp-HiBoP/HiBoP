using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    #region Properties
    [SerializeField]
    private Canvas m_Canvas;
    public Canvas Canvas
    {
        get { return m_Canvas; }
        set { m_Canvas = value; }
    }

    [SerializeField]
    GameObject LoadingCirclePrefab;
    #endregion

    #region Public Methods
    public LoadingCircle Open()
    {
        GameObject loadingCircleGameObject = Instantiate(LoadingCirclePrefab, Canvas.transform);
        LoadingCircle loadingCircle = loadingCircleGameObject.GetComponent<LoadingCircle>();
        return loadingCircle;
    }
    #endregion
}
