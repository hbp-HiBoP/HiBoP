using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageAspectFitter : MonoBehaviour, ILayoutElement
{
    #region Properties
    [SerializeField] public float minWidth { get; private set; }
    [SerializeField] public float preferredWidth { get; private set; }
    [SerializeField] public float flexibleWidth { get; private set; }

    [SerializeField] public float minHeight { get; private set; }
    [SerializeField] public float preferredHeight { get; private set; }
    [SerializeField] public float flexibleHeight { get; private set; }

    [SerializeField] public int layoutPriority { get; private set; }

    public enum AspectModeEnum { None, WidthControlsHeigth,HeigthControlsWidth}
    [SerializeField] public AspectModeEnum AspectMode { get; set; }
    #endregion

    #region Public Methods
    public void CalculateLayoutInputHorizontal()
    {
        if(AspectMode == AspectModeEnum.HeigthControlsWidth)
        {
            Image image = GetComponent<Image>();
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            float ratio = image.mainTexture.width / (float)image.mainTexture.height;
            layoutPriority = 1;
            minWidth = 0;
            preferredWidth = ratio * rectTransform.rect.height;
            flexibleWidth = 0;
        }
    }
    public void CalculateLayoutInputVertical()
    {
        if (AspectMode == AspectModeEnum.WidthControlsHeigth)
        {
            Image image = GetComponent<Image>();
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            float ratio = (float)image.mainTexture.height / image.mainTexture.width;
            layoutPriority = 1;
            minHeight = 0;
            preferredHeight = ratio * rectTransform.rect.width;
            flexibleHeight = 0;
        }
    }
    #endregion
}
