using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using d = Tools.Unity.Graph.Data;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Display gestion.
        /// </summary>
        DisplayGestion m_displayGestion;

        /// <summary>
        /// Informations gestion.
        /// </summary>
        InformationsGestion m_informationsGestion;

        /// <summary>
        /// Curves of the graph.
        /// </summary>
        public d.Curve[] Curves { get { return m_displayGestion.Curves; } }

        /// <summary>
        /// Graph title.
        /// </summary>
        public string Title { get { return m_informationsGestion.Title; } set { m_informationsGestion.Title = value; } }

        /// <summary>
        /// Abscissa label.
        /// </summary>
        public string Abscissa { get { return m_informationsGestion.Abscissa; } set { m_informationsGestion.Abscissa = value; } }

        /// <summary>
        /// Ordinate label.
        /// </summary>
        public string Ordinate { get { return m_informationsGestion.Ordinate; } set { m_informationsGestion.Ordinate = value; } }

        Color m_backGroundColor;
        /// <summary>
        /// Background color of the graph.
        /// </summary>
        public Color BackGroundColor { get { return m_backGroundColor; } set { SetBackGroundColor(value); } }

        Color m_fontColor;
        /// <summary>
        /// Font color of the graph.
        /// </summary>
        public Color FontColor { get { return m_fontColor; } set { SetFontColor(value); } }

        /// <summary>
        /// Abcissa Window.
        /// </summary>
        public Vector2 AbcissaWindow { get { return m_displayGestion.XWindow ; } }

        /// <summary>
        /// Ordinate Window.
        /// </summary>
        public Vector2 OrdinateWindow { get { return m_displayGestion.YWindow ; } }

        /// <summary>
        /// Event called when a display window is set manually.
        /// </summary>
        public UnityEvent SetWindowEvent{ get { return m_displayGestion.SetWindowEvent; } }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the graph.
        /// </summary>
        /// <param name="title">Title of the graph.</param>
        /// <param name="abcissa">Unite of the absciss axe of the graph.</param>
        /// <param name="ordinate">Unite of the ordinate axe of the graph.</param>
        /// <param name="backGroundColor">BackGround color of the graph.</param>
        /// <param name="fontColor">Font color of the graph.</param>
        public void Set(string title, string abcissa, string ordinate, Color backGroundColor, Color fontColor)
        {
            Title = title;
            Abscissa = abcissa;
            Ordinate = ordinate;
            BackGroundColor = backGroundColor;
            FontColor = fontColor;
        }

        public void Display(d.Curve[] curves,Vector2 xWindow,Vector2 yWindow,bool interactable)
        {
            m_displayGestion.Display(curves, xWindow, yWindow, interactable);
            m_informationsGestion.Curves = curves;
        }

        #endregion

        #region Private Methods

        void Awake()
        {
            m_displayGestion = GetComponentInChildren<DisplayGestion>();
            m_informationsGestion = GetComponentInChildren<InformationsGestion>();
        }

        #region Getter/Setter
        void SetBackGroundColor(Color color)
        {
            m_backGroundColor = color;
            m_displayGestion.GetComponent<Image>().color = m_backGroundColor;
        }

        void SetFontColor(Color color)
        {
            m_fontColor = color;
            m_informationsGestion.Color = color;
            m_displayGestion.AxesColor = color;
        }
        #endregion
        #endregion
    }
}
