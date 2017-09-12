using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Selectable))]
public class InteractableDisplayer : MonoBehaviour
{
    #region Properties
    public Graphic[] Graphics;
    public Color Color = new Color(100, 100, 100, 255) / 255.0f;

    Dictionary<Graphic, Color> m_ColorByGraphic = new Dictionary<Graphic, Color>();
    Selectable m_Selectable;
    ThemeElement m_ThemeElement;
    bool m_LastState;
    #endregion

    #region Private Methods
    private void Awake()
    {
        m_Selectable = GetComponent<Selectable>();
        m_ThemeElement = GetComponent<ThemeElement>();
        UpdateColor(m_Selectable.interactable);
    }

    void Update()
    {
        if(m_Selectable.interactable != m_LastState)
        {
            UpdateColor(m_Selectable.interactable);
        }
    }
    void UpdateColor(bool interactable)
    {
        if (interactable)
        {
            if (m_ThemeElement != null)
            {
                m_ThemeElement.IgnoreTheme = false;
            }
            foreach (Graphic graphic in Graphics)
            {
                if(m_ColorByGraphic.ContainsKey(graphic))
                {
                    graphic.color = m_ColorByGraphic[graphic];
                    m_ColorByGraphic.Remove(graphic);
                }
            }
        }
        else
        {
            if(m_ThemeElement != null)
            {
                m_ThemeElement.IgnoreTheme = true;
            }
            foreach (Graphic graphique in Graphics)
            {
                if(!m_ColorByGraphic.ContainsKey(graphique))
                {
                    m_ColorByGraphic.Add(graphique, graphique.color);
                }
                graphique.color = Color;
            }
        }
        m_LastState = interactable;
    }
#endregion
}
