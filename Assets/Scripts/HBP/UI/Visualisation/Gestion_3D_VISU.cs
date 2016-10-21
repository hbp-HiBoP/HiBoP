using UnityEngine;

public class Gestion_3D_VISU : MonoBehaviour
{
    #region Properties
    public HBP.UI.HBPLinker Module3D;
    #endregion

    #region Public Methods
    void Start()
    {
        Update3DPanel();
    }

    public void OnRectTransformDimensionsChange()
    {
        Update3DPanel();
    }
    #endregion

    #region Private Methods
    void Update3DPanel()
    {
        RectTransform l_screen = transform as RectTransform;
        //Debug.Log("X : " + (l_screen.position.x + l_screen.rect.xMin));
        //Debug.Log("Y : " + (l_screen.position.y + l_screen.rect.yMin));
        //Debug.Log("Width : " + l_screen.rect.width);
        //Debug.Log("Height : " + l_screen.rect.height);

        Module3D.BackgroundCamera.pixelRect = new Rect(l_screen.position.x + l_screen.rect.xMin, l_screen.position.y + l_screen.rect.yMin, l_screen.rect.width, l_screen.rect.height);
    }

    void Update()
    {
        if(transform.hasChanged)
        {
            Update3DPanel();
            transform.hasChanged = false;
        }
    }
    #endregion
}
