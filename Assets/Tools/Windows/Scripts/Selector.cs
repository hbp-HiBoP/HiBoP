using UnityEngine;
using UnityEngine.UI;

public class Selector : MonoBehaviour
{
    #region Properties
    [SerializeField, Candlelight.PropertyBackingField]
    private bool m_Selected;
    public bool Selected
    {
        get { return m_Selected; }
        set { m_Selected = value; m_TargetGraphic.gameObject.SetActive(value); }
    }

    [SerializeField, Candlelight.PropertyBackingField]
    private Graphic m_TargetGraphic;
    public Graphic TargetGraphic
    {
        get { return m_TargetGraphic; }
        set { m_TargetGraphic = value; }
    }
    #endregion

    #region Private Methods
    void Start()
    {
    }
    #endregion
}
