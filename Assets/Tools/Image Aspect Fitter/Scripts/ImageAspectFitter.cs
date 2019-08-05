using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

[ExecuteInEditMode]
[AddComponentMenu("Layout/Image Aspect Fitter", 141)]
[RequireComponent(typeof(Image), typeof(RectTransform))]
public class ImageAspectFitter : UIBehaviour, ILayoutElement, ILayoutIgnorer
{
    #region Properties
    public enum FitMode { None, WidthControlsHeigth, HeigthControlsWidth, MaxControlMin}
    [SerializeField]
    private FitMode m_FitMode;

    [SerializeField]
    private float m_MinWidth = -1f;

    [SerializeField]
    private float m_MinHeight = -1f;

    [SerializeField]
    private float m_PreferredWidth = -1f;

    [SerializeField]
    private float m_PreferredHeight = -1f;

    [SerializeField]
    private float m_FlexibleWidth = -1f;

    [SerializeField]
    private float m_FlexibleHeight = -1f;

    [SerializeField]
    private float m_Min = -1f;

    [SerializeField]
    private float m_Preferred = -1f;

    [SerializeField]
    private float m_Flexible = -1f;

    [SerializeField]
    private bool m_IgnoreLayout;

    protected Image m_Image;
    protected RectTransform m_RectTransform;

    /// <summary>
    ///   <para>Should this RectTransform be ignored by the layout system?</para>
    /// </summary>
    public virtual bool ignoreLayout
    {
        get
        {
            return this.m_IgnoreLayout;
        }
        set
        {
            if (!SetPropertyUtility.SetStruct<bool>(ref this.m_IgnoreLayout, value))
                return;
            this.SetDirty();
        }
    }

    public FitMode fitMode
    {
        get
        {
            return this.m_FitMode;
        }
        set
        {
            if (!SetPropertyUtility.SetStruct<FitMode>(ref this.m_FitMode, value))
                return;
            this.SetDirty();
        }
    }

    /// <summary>
    ///   <para>The minimum width this layout element may be allocated.</para>
    /// </summary>
    public virtual float minWidth
    {
        get
        {
            return this.m_MinWidth;
        }
    }

    /// <summary>
    ///   <para>The minimum height this layout element may be allocated.</para>
    /// </summary>
    public virtual float minHeight
    {
        get
        {
            return this.m_MinHeight;
        }
    }

    /// <summary>
    ///   <para>The preferred width this layout element should be allocated if there is sufficient space.</para>
    /// </summary>
    public virtual float preferredWidth
    {
        get
        {
            return this.m_PreferredWidth;
        }
    }

    /// <summary>
    ///   <para>The preferred height this layout element should be allocated if there is sufficient space.</para>
    /// </summary>
    public virtual float preferredHeight
    {
        get
        {
            return this.m_PreferredHeight;
        }
    }

    /// <summary>
    ///   <para>The extra relative width this layout element should be allocated if there is additional available space.</para>
    /// </summary>
    public virtual float flexibleWidth
    {
        get
        {
            return this.m_FlexibleWidth;
        }
    }

    /// <summary>
    ///   <para>The extra relative height this layout element should be allocated if there is additional available space.</para>
    /// </summary>
    public virtual float flexibleHeight
    {
        get
        {
            return this.m_FlexibleHeight;
        }
    }

    protected ImageAspectFitter()
    {
    }

    /// <summary>
    ///   <para>Called by the layout system.</para>
    /// </summary>
    public virtual int layoutPriority
    {
        get
        {
            return 1;
        }
    }

    #endregion

    #region Public Methods
    public void CalculateLayoutInputHorizontal()
    {
        float ratio = m_Image.mainTexture.width / (float)m_Image.mainTexture.height;
        if (m_FitMode == FitMode.HeigthControlsWidth || (m_FitMode == FitMode.MaxControlMin && ratio < 1))
        {
            m_MinWidth = m_Min == -1f? m_Min : ratio * m_Min;
            m_PreferredWidth = m_Preferred == -1f? m_Preferred : ratio * m_Preferred;
            m_FlexibleWidth = m_Flexible == -1f? m_Flexible : ratio * m_Flexible;

            m_MinHeight = m_Min;
            m_PreferredHeight = m_Preferred;
            m_FlexibleHeight = m_Flexible;
        }
    }
    public void CalculateLayoutInputVertical()
    {
        float ratio = (float)m_Image.mainTexture.height / m_Image.mainTexture.width;
        if (m_FitMode == FitMode.WidthControlsHeigth || (m_FitMode == FitMode.MaxControlMin && ratio < 1))
        {
            m_MinWidth = m_Min;
            m_PreferredWidth = m_Preferred;
            m_FlexibleWidth = m_Flexible;

            m_MinHeight = m_Min == -1f? m_Min : ratio * m_Min;
            m_PreferredHeight = m_Preferred == -1? m_Preferred : ratio * m_Preferred;
            m_FlexibleHeight = m_Flexible == -1? m_Flexible : ratio * m_Flexible;
        }
    }
    #endregion

    #region Protected Methods
    protected override void OnEnable()
    {
        base.OnEnable();
        m_Image = GetComponent<Image>();
        m_RectTransform = GetComponent<RectTransform>();
        this.SetDirty();
    }

    protected override void OnTransformParentChanged()
    {
        this.SetDirty();
    }
    /// <summary>
    ///   <para>See MonoBehaviour.OnDisable.</para>
    /// </summary>
    protected override void OnDisable()
    {
        this.SetDirty();
        base.OnDisable();
    }
    protected override void OnDidApplyAnimationProperties()
    {
        this.SetDirty();
    }

    protected override void OnBeforeTransformParentChanged()
    {
        this.SetDirty();
    }
    /// <summary>
    ///   <para>Mark the LayoutElement as dirty.</para>
    /// </summary>
    protected void SetDirty()
    {
        if (!this.IsActive())
            return;
        LayoutRebuilder.MarkLayoutForRebuild(this.transform as RectTransform);
    }

    protected override void OnValidate()
    {
        m_Image = GetComponent<Image>();
        m_RectTransform = GetComponent<RectTransform>();
        this.SetDirty();
    }
    #endregion
}
