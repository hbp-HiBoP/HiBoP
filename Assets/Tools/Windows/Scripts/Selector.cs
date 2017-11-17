using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Selector : MonoBehaviour
{
    #region Properties
    [SerializeField, Candlelight.PropertyBackingField]
    private bool m_Selected;
    public bool Selected
    {
        get { return m_Selected; }
        set
        {
            m_Selected = value;
            m_TargetGraphic.gameObject.SetActive(value);
            OnChangeValue.Invoke(value);
            if(value) transform.SetAsLastSibling();
        }
    }
    public GenericEvent<bool> OnChangeValue = new GenericEvent<bool>();

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
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager) selectionManager.Add(this);
    }
    private void OnDestroy()
    {
        SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
        if (selectionManager) selectionManager.Remove(this);
    }
    #endregion
}
