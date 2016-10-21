using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using d = Tools.Unity.Graph.Data;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(RectTransform))]
    public class DisplayGestion : MonoBehaviour
    {
        #region Properties
        RectTransform m_rect;

        [SerializeField]
        GameObject m_curve;
        [SerializeField]
        GameObject m_curveWithStandardDeviation;

        [SerializeField]
        WindowInputGestion m_windowInputGestion;

        [SerializeField]
        Axe m_absciss;
        [SerializeField]
        Axe m_ordinate;

        Color m_axeColor = Color.white;
        public Color AxesColor { set { m_axeColor = value; } get { return m_axeColor; } }

        List<d.Curve> m_curvesDisplayed = new List<d.Curve>();
        public d.Curve[] Curves { get { return m_curvesDisplayed.ToArray(); } }

        List<Curve> m_curveDisplayers = new List<Curve>();

        Vector2 m_ratio;

        Vector2 m_xWindow;
        public Vector2 XWindow { get { return m_xWindow; } set { SetXWindow(value); } }

        Vector2 m_yWindow;
        public Vector2 YWindow { get { return m_yWindow; } set { SetYWindow(value);} }

        bool m_interactable;

        public UnityEvent SetWindowEvent = new UnityEvent();

        #endregion

        #region Public Methods

        public void Display(d.Curve[] curves, Vector2 xWindow, Vector2 yWindow, bool interactable)
        {
            XWindow = xWindow;
            YWindow = yWindow;
            UpdateAxes();

            List<d.Curve> l_curvesToDisplay = new List<d.Curve>(curves);
            List<d.Curve> l_curvesToAdd = new List<d.Curve>();
            List<d.Curve> l_curvesToRemove = new List<d.Curve>();

            // Curves to remove
            foreach (Curve curveDisplayer in m_curveDisplayers)
            {
                if (!l_curvesToDisplay.Contains(curveDisplayer.DataCurve))
                {
                    l_curvesToRemove.Add(curveDisplayer.DataCurve);
                }
            }

            // Curves to add
            foreach (d.Curve curve in l_curvesToDisplay)
            {
                if (!m_curvesDisplayed.Contains(curve))
                {
                    l_curvesToAdd.Add(curve);
                }
            }

            // Remove curves
            foreach (d.Curve curve in l_curvesToRemove)
            {
                RemoveCurve(curve);
            }

            // Update curves
            foreach (Curve curveDisplayer in m_curveDisplayers)
            {
                UpdateCurve(curveDisplayer);
            }

            // Add curves
            foreach (d.Curve curve in l_curvesToAdd)
            {
                AddCurve(curve);
            }
        }

        public void ChangeWindowSize(Vector2 x,Vector2 y)
        {
            //Call Event
            SetWindowEvent.Invoke();

            // Set Windows
            XWindow = x;
            YWindow = y;
            UpdateDisplay(); 
        }

        public void Move(Vector2 command)
        {
            float l_addX = (XWindow.x - XWindow.y) * command.x;
            float l_addY = (YWindow.x - YWindow.y) * command.y;
            XWindow += l_addX * Vector2.one;
            YWindow += l_addY * Vector2.one;
            UpdateDisplay();
        }

        public void ChangeRectSize(Vector2 command)
        {
            float l_x = XWindow.x - XWindow.y;
            XWindow += new Vector2(0, 0);
            YWindow += new Vector2(0, 0);
            StartCoroutine(c_UpdateDisplay());
        }

        public void Zoom()
        {
            float l_width = (XWindow.y - XWindow.x)*0.05f;
            float l_height = (YWindow.y - YWindow.x)*0.05f;
            XWindow += new Vector2(l_width,-l_width);
            YWindow += new Vector2(l_height, -l_height);
            UpdateDisplay();
        }

        public void Dezoom()
        {
            float l_width = (XWindow.y - XWindow.x) * 0.05f;
            float l_height = (YWindow.y - YWindow.x) * 0.05f;
            XWindow += new Vector2(-l_width,l_width);
            YWindow += new Vector2(-l_height, l_height);
            UpdateDisplay();
        }
        #endregion

        #region Private Methods

        void AddCurve(d.Curve curve)
        {
            GameObject l_curve;
            if (curve is d.CurveWithStandardDeviation)
            {
                l_curve = Instantiate(m_curveWithStandardDeviation);
            }
            else
            {
                l_curve = Instantiate(m_curve);
            }
            Curve l_curveDisplayer = l_curve.GetComponent<Curve>();
            l_curve.transform.SetParent(transform.GetChild(1));
            RectTransform l_rect = l_curve.transform as RectTransform;
            l_rect.offsetMin = Vector2.zero;
            l_rect.offsetMax = Vector2.zero;
            Vector2 origin = new Vector2(XWindow.x, YWindow.x);
            l_curveDisplayer.Set(curve, origin, m_ratio);
            m_curvesDisplayed.Add(curve);
            m_curveDisplayers.Add(l_curveDisplayer);

        }

        void RemoveCurve(d.Curve curve)
        {
            for (int i = 0; i < m_curveDisplayers.Count; i++)
            {
                if(m_curveDisplayers[i].DataCurve == curve)
                {
                    Destroy(m_curveDisplayers[i].gameObject);
                    m_curveDisplayers.RemoveAt(i);
                    m_curvesDisplayed.Remove(curve);
                    break;
                }
            }
        }

        public void OpenWindow()
        {
            m_windowInputGestion.gameObject.SetActive(true);
        }

        public void CloseWindow()
        {
            m_windowInputGestion.gameObject.SetActive(false);
        }

        void SetXWindow(Vector2 window)
        {
            m_xWindow = window;
            m_ratio.x = m_rect.rect.width / (XWindow.y - XWindow.x);
            m_windowInputGestion.SetFields(XWindow, YWindow);
        }

        void SetYWindow(Vector2 window)
        {
            m_yWindow = window;
            m_ratio.y = m_rect.rect.height / (YWindow.y - YWindow.x);
            m_windowInputGestion.SetFields(XWindow, YWindow);
        }

        void Awake()
        {
            m_rect = GetComponent<RectTransform>();
        }

        void UpdateCurves()
        {
            foreach(Curve curve in m_curveDisplayers)
            {
                UpdateCurve(curve);
            }
        }

        void UpdateAxes()
        {
            m_absciss.Set(AxesColor, XWindow.x, XWindow.y);
            m_ordinate.Set(AxesColor, YWindow.x, YWindow.y);
            foreach (Transform tr in transform.GetChild(0).GetChild(0))
            {
                Destroy(tr.gameObject);
            }
            foreach (Transform tr in transform.GetChild(0).GetChild(1))
            {
                Destroy(tr.gameObject);
            }

            if ((XWindow.x < 0 && XWindow.y > 0) || (XWindow.x > 0 && XWindow.y < 0))
            {
                GameObject l_line = new GameObject();
                l_line.name = "0";
                l_line.transform.SetParent(transform.GetChild(0).GetChild(0));
                UnityEngine.UI.Image l_image = l_line.AddComponent<UnityEngine.UI.Image>();
                l_image.color = AxesColor;
                RectTransform l_rectTransform = l_line.transform as RectTransform;
                l_rectTransform.anchorMin = new Vector2(0, 0);
                l_rectTransform.anchorMax = new Vector2(0, 1);
                float l_position = -XWindow.x * m_ratio.x;
                l_rectTransform.offsetMax = new Vector2(l_position, 0);
                l_rectTransform.offsetMin = new Vector2(l_position, 0);
                l_rectTransform.sizeDelta = new Vector2(2, l_rectTransform.sizeDelta.y);
            }
            if ((YWindow.x < 0 && YWindow.y > 0) || (YWindow.x > 0 && YWindow.y < 0))
            {
                GameObject l_line = new GameObject();
                l_line.name = "0";
                l_line.transform.SetParent(transform.GetChild(0).GetChild(1));
                UnityEngine.UI.Image l_image = l_line.AddComponent<UnityEngine.UI.Image>();
                l_image.color = AxesColor;
                RectTransform l_rectTransform = l_line.transform as RectTransform;
                l_rectTransform.anchorMin = new Vector2(0, 0);
                l_rectTransform.anchorMax = new Vector2(1, 0);
                float l_position = -YWindow.x * m_ratio.y;
                l_rectTransform.offsetMax = new Vector2(0, l_position);
                l_rectTransform.offsetMin = new Vector2(0, l_position);
                l_rectTransform.sizeDelta = new Vector2(l_rectTransform.sizeDelta.x, 2);
            }
        }

        void UpdateDisplay()
        {
           UpdateCurves();
           UpdateAxes();
        }

        IEnumerator c_UpdateDisplay()
        {
            yield return new WaitForEndOfFrame();
            UpdateDisplay();
        }

        void UpdateCurve(Curve curveToUpdate)
        {
            Vector2 origin = new Vector2(XWindow.x, YWindow.x);
            curveToUpdate.Zoom(m_ratio);
            curveToUpdate.Move(origin);
        }
        #endregion
    }
}