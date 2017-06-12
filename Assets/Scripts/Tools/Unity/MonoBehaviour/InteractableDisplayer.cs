using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Selectable))]
public class InteractableDisplayer : MonoBehaviour
{
    #region Properties
    public Graphic[] Graphics;
    public Color Color;

    Dictionary<Graphic, Color> m_ColorByGraphic = new Dictionary<Graphic, Color>();
    Selectable m_Selectable;
    bool m_LastState;
    #endregion

    #region Private Methods
    void OnEnable()
    {
        m_Selectable = GetComponent<Selectable>();
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
