using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Selectable))]
public class InteractableDisplayer : MonoBehaviour
{
    #region Properties
    Selectable m_Selectable;
    HBP.UI.Theme.ThemeElement m_ThemeElement;
    bool m_LastState;
    #endregion

    #region Private Methods
    private void Awake()
    {
        m_Selectable = GetComponent<Selectable>();
        m_ThemeElement = GetComponent<HBP.UI.Theme.ThemeElement>();
        m_ThemeElement.Set(ApplicationState.Theme);
    }

    void Update()
    {
        if (m_Selectable.interactable != m_LastState)
        {
            m_ThemeElement.Set(ApplicationState.Theme);
        }
    }
    #endregion
}
