using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;

namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    [AddComponentMenu("Layout/Content Size Clamper", 142)]
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeClamper : UIBehaviour, ILayoutController, ILayoutSelfController
    {
        #region Properties
        protected DrivenRectTransformTracker m_Tracker;

        public enum ClampMode
        {
            Unconstrained,
            MinSize,
            PreferredSize,
            Custom,
        }

        [SerializeField] protected ClampMode m_MaxHorizontalClamp;
        public ClampMode MaxHorizontalClamp
        {
            get
            {
                return m_MaxHorizontalClamp;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MaxHorizontalClamp, value))
                    return;
                SetDirty();
            }
        }
        [SerializeField] protected float m_MaxHorizontalCustomValue = float.MaxValue;
        public float MaxHorizontalCustomValue
        {
            get
            {
                return m_MaxHorizontalCustomValue;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MaxHorizontalCustomValue, value))
                    return;
                SetDirty();
            }
        }

        [SerializeField] protected ClampMode m_MinHorizontalClamp;
        public ClampMode MinHorizontalClamp
        {
            get
            {
                return m_MinHorizontalClamp;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MinHorizontalClamp, value))
                    return;
                SetDirty();
            }
        }
        [SerializeField] protected float m_MinHorizontalCustomValue = 0;
        public float MinHorizontalCustomValue
        {
            get
            {
                return m_MinHorizontalCustomValue;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MinHorizontalCustomValue, value))
                    return;
                SetDirty();
            }
        }

        [SerializeField] protected ClampMode m_MaxVerticalClamp;
        public ClampMode MaxVerticalClamp
        {
            get
            {
                return m_MaxVerticalClamp;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MaxVerticalClamp, value))
                    return;
                SetDirty();
            }
        }
        [SerializeField] protected float m_MaxVerticalCustomValue = float.MaxValue;
        public float MaxVerticalCustomValue
        {
            get
            {
                return m_MaxVerticalCustomValue;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MaxVerticalCustomValue, value))
                    return;
                SetDirty();
            }
        }

        [SerializeField] protected ClampMode m_MinVerticalClamp;
        public ClampMode MinVerticalClamp
        {
            get
            {
                return m_MinVerticalClamp;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MinVerticalClamp, value))
                    return;
                SetDirty();
            }
        }
        [SerializeField] protected float m_MinVerticalCustomValue = 0;
        public float MinVerticalCustomValue
        {
            get
            {
                return m_MinVerticalCustomValue;
            }
            set
            {
                if (!SetPropertyUtility.SetStruct(ref m_MinVerticalCustomValue, value))
                    return;
                SetDirty();
            }
        }

        [NonSerialized] private RectTransform m_RectTransform;
        protected RectTransform RectTransform
        {
            get
            {
                if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
                return m_RectTransform;
            }
        }
        #endregion

        #region Constructors
        protected ContentSizeClamper()
        {
        }
        #endregion

        #region Public Methods
        /// <summary>
        ///   <para>Method called by the layout system.</para>
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }
        /// <summary>
        ///   <para>Method called by the layout system.</para>
        /// </summary>
        public virtual void SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }
        #endregion

        #region Protected Methods
        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }
        /// <summary>
        ///   <para>See MonoBehaviour.OnDisable.</para>
        /// </summary>
        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            base.OnDisable();
        }
        protected override void OnRectTransformDimensionsChange()
        {
            this.SetDirty();
        }
        protected void HandleSelfFittingAlongAxis(int axis)
        {
            //if (RectTransform.rect.width == 0 && RectTransform.rect.height == 0) return;
            ClampMode minClampMode = axis != 0 ? MinVerticalClamp : MinHorizontalClamp;
            ClampMode maxClampMode = axis != 0 ? MaxVerticalClamp : MaxHorizontalClamp;
            float min = float.MinValue, max = float.MaxValue;
            switch (minClampMode)
            {
                case ClampMode.MinSize:
                    min = LayoutUtility.GetMinSize(m_RectTransform, axis);
                    break;
                case ClampMode.PreferredSize:
                    min = LayoutUtility.GetPreferredSize(m_RectTransform, axis);
                    break;
                case ClampMode.Custom:
                    min = axis != 0 ? MinVerticalCustomValue : MinHorizontalCustomValue;
                    break;
            }
            switch (maxClampMode)
            {
                case ClampMode.MinSize:
                    max = LayoutUtility.GetMinSize(m_RectTransform, axis);
                    break;
                case ClampMode.PreferredSize:
                    max = LayoutUtility.GetPreferredSize(m_RectTransform, axis);
                    break;
                case ClampMode.Custom:
                    max = axis != 0 ? MaxVerticalCustomValue : MaxHorizontalCustomValue;
                    break;
            }

            float actualSize = axis != 0 ? RectTransform.rect.height : RectTransform.rect.width;
            if (max < min) max = min;
            float size = Mathf.Clamp(actualSize, min, max);
            RectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, size);
            //if (minClampMode == ClampMode.Unconstrained && maxClampMode == ClampMode.Unconstrained)
            //{
            //    m_Tracker.Add(this, RectTransform, DrivenTransformProperties.None);
            //}
            //else
            //{
            //    m_Tracker.Add(this, RectTransform, axis != 0 ? DrivenTransformProperties.SizeDeltaY : DrivenTransformProperties.SizeDeltaX);
            //}
        }
        /// <summary>
        ///   <para>Mark the ContentSizeFitter as dirty.</para>
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
        #endregion
    }
}