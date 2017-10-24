using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageRatio : LayoutElement
{
    #region Properties
    Sprite m_LastSprite;
    Image m_Image;
    #endregion

    #region Public Methods
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        Set();
    }
    public override void CalculateLayoutInputVertical()
    {
        base.CalculateLayoutInputVertical();
        Set();
    }
    #endregion

    #region Private Methods
    protected override void OnEnable()
    {
        m_Image = GetComponent<Image>();
    }
    void Update ()
	{
        if (m_LastSprite != m_Image.sprite) Set();
    }
    void Set()
    {
        float ratio = (float) m_Image.sprite.texture.height / m_Image.sprite.texture.width;
        float height = 0;
        if(flexibleHeight == 0)
        {
            height = preferredHeight;
        }
        else
        {
            height = (transform as RectTransform).rect.height;
        }
        float result = height / ratio;
        minWidth = result / ratio;
        preferredWidth = minWidth;
    }
	#endregion
}