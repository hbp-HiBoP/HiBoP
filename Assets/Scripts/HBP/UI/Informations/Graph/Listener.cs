using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Informations.Graphs
{
    [RequireComponent(typeof(RectTransform))]
    public class Listener : MonoBehaviour
    {
        #region Properties
        [SerializeField] Vector2 m_AbscissaDisplayRange;
        public Vector2 AbscissaDisplayRange
        {
            get
            {
                return m_AbscissaDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_AbscissaDisplayRange, value))
                {
                    SetAbscissaDisplayRange();
                }
            }
        }

        [SerializeField] Vector2 m_OrdinateDisplayRange;
        public Vector2 OrdinateDisplayRange
        {
            get
            {
                return m_OrdinateDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_OrdinateDisplayRange, value))
                {
                    SetOrdinateDisplayRange();
                }
            }
        }

        RectTransform m_RectTransform;
        float m_LastHeight, m_LastWidth;
        Vector2 m_MouseLastPosition;
        bool m_FirstUse = true;

        [SerializeField] float m_ZoomSpeed = 0.05f;
        public float ZoomSpeed
        {
            get
            {
                return m_ZoomSpeed;
            }
            set
            {
                SetPropertyUtility.SetStruct(ref m_ZoomSpeed, value);
            }
        }

        #region Events
        [SerializeField] private Vector2Event m_OnChangeAbscissaDisplayRange;
        public Vector2Event OnChangeAbscissaDisplayRange
        {
            get
            {
                return m_OnChangeAbscissaDisplayRange;
            }
        }
        [SerializeField] private Vector2Event m_OnChangeOrdinateDisplayRange;
        public Vector2Event OnChangeOrdinateDisplayRange
        {
            get
            {
                return m_OnChangeOrdinateDisplayRange;
            }
        }
        #endregion
        #endregion

        #region Public Methods
        public void OnBeginDrag()
        {
            m_MouseLastPosition = Input.mousePosition;
        }
        public void OnDrag()
        {
            Vector2 mouseActualPosition = Input.mousePosition;
            Vector2 displacement = mouseActualPosition - m_MouseLastPosition;
            Vector2 proportionnalDisplacement = new Vector2(displacement.x / m_RectTransform.rect.width, displacement.y / m_RectTransform.rect.height);
            Move(proportionnalDisplacement);
            m_MouseLastPosition = mouseActualPosition;
        }
        public void OnScroll()
        {
            float delta = Input.mouseScrollDelta.y;
            if (delta > 0) Zoom();
            else if(delta < 0) Dezoom();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_LastHeight = m_RectTransform.rect.height;
            m_LastWidth = m_RectTransform.rect.width;
        }
        void OnRectTransformDimensionsChange()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(c_OnRect());
            }
        }
        void SetOrdinateDisplayRange()
        {
            OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void SetAbscissaDisplayRange()
        {
            OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
        }

        IEnumerator c_OnRect()
        {
            yield return new WaitForEndOfFrame();
            OnRect();
        }
        void OnRect()
        {
            Rect rect = m_RectTransform.rect;
            if (m_LastHeight != rect.height || m_LastWidth != rect.width)
            {
                if (m_FirstUse)
                {
                    m_LastHeight = rect.height;
                    m_LastWidth = rect.width;
                    m_FirstUse = false;
                }
                else
                {
                    Vector2 command = Vector2.zero;
                    command.x = (rect.width - m_LastWidth) / m_LastWidth;
                    command.y = (rect.height - m_LastHeight) / m_LastHeight;
                    m_LastHeight = rect.height;
                    m_LastWidth = rect.width;
                    ChangeRectSize(command);
                }
            }
        }

        void Move(Vector2 command)
        {
            if(command.x != 0)
            {
                m_AbscissaDisplayRange += (m_AbscissaDisplayRange.x - m_AbscissaDisplayRange.y) * command.x * Vector2.one;
                OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            }
            if (command.y != 0)
            {
                m_OrdinateDisplayRange += (m_OrdinateDisplayRange.x - m_OrdinateDisplayRange.y) * command.y * Vector2.one;
                OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
            }
        }
        void Zoom()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, Input.mousePosition, null, out Vector2 localPoint);
            Vector2 ratio = localPoint / m_RectTransform.rect.size + m_RectTransform.pivot;
            Vector2 zoom = m_ZoomSpeed * new Vector2(m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x, m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);

            m_AbscissaDisplayRange.x += ratio.x * zoom.x;
            m_AbscissaDisplayRange.y -= (1 - ratio.x) * zoom.x;
            m_OrdinateDisplayRange.x += ratio.y * zoom.y;
            m_OrdinateDisplayRange.y -= (1 - ratio.y) * zoom.y;

            OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void Dezoom()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, Input.mousePosition, null, out Vector2 localPoint);
            Vector2 ratio = localPoint / m_RectTransform.rect.size + m_RectTransform.pivot;
            Vector2 dezoom = new Vector2(m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x, m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x) * (m_ZoomSpeed / (1 - m_ZoomSpeed));

            m_AbscissaDisplayRange.x -= ratio.x * dezoom.x;
            m_AbscissaDisplayRange.y += (1 - ratio.x) * dezoom.x;
            m_OrdinateDisplayRange.x -= ratio.y * dezoom.y;
            m_OrdinateDisplayRange.y += (1 - ratio.y) * dezoom.y;

            OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void ChangeRectSize(Vector2 command)
        {
            OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        #endregion
    }
}