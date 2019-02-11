using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
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
            float l_scroll = Input.mouseScrollDelta.y;
            if (l_scroll > 0)
            {
                Zoom();
            }
            else if (l_scroll < 0)
            {
                Dezoom();
            }
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
                    Vector2 l_command = Vector2.zero;
                    l_command.x = (rect.width - m_LastWidth) / m_LastWidth;
                    l_command.y = (rect.height - m_LastHeight) / m_LastHeight;
                    m_LastHeight = rect.height;
                    m_LastWidth = rect.width;
                    ChangeRectSize(l_command);
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
            Vector2 mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, mousePosition, null, out Vector2 localPoint);
            Vector2 ratio = localPoint / m_RectTransform.rect.size + m_RectTransform.pivot;
            Vector2 offsetRatio = ratio - new Vector2(0.5f, 0.5f);
            Vector2 offset = new Vector2(offsetRatio.x * (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x), offsetRatio.y * (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x));
            Vector2 zoom = m_ZoomSpeed * new Vector2(m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x, m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
            Debug.Log(offset);
            if(offset.x > 0)
            {
                float offsetZoom = Mathf.Min( 2 * offset.x, zoom.x);
                float restZoom = zoom.x - offsetZoom;
                float leftZoom = offsetZoom + restZoom / 2;
                float rightZoom = restZoom / 2;
                m_AbscissaDisplayRange.x += leftZoom;
                m_AbscissaDisplayRange.y -= rightZoom;
            }
            else
            {
                float offsetZoom = Mathf.Min(2 * Mathf.Abs(offset.x), zoom.x);
                float restZoom = zoom.x - offsetZoom;
                float leftZoom = restZoom / 2;
                float rightZoom = offsetZoom + restZoom / 2;
                m_AbscissaDisplayRange.x += leftZoom;
                m_AbscissaDisplayRange.y -= rightZoom;
            }

            if (offset.y > 0)
            {
                float offsetZoom = Mathf.Min(2 * offset.y, zoom.y);
                float restZoom = zoom.y - offsetZoom;
                float leftZoom = offsetZoom + restZoom / 2;
                float rightZoom = restZoom / 2;
                m_OrdinateDisplayRange.x += leftZoom;
                m_OrdinateDisplayRange.y -= rightZoom;
            }
            else
            {
                float offsetZoom = Mathf.Min(2 * Mathf.Abs(offset.y), zoom.y);
                float restZoom = zoom.y - offsetZoom;
                float leftZoom = restZoom / 2;
                float rightZoom = offsetZoom + restZoom / 2;
                m_OrdinateDisplayRange.x += leftZoom;
                m_OrdinateDisplayRange.y -= rightZoom;
            }
            OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void Dezoom()
        {
            m_AbscissaDisplayRange += (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x) * m_ZoomSpeed * new Vector2(-0.5f, 0.5f);
            m_OrdinateDisplayRange += (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x) * m_ZoomSpeed * new Vector2(-0.5f, 0.5f);
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