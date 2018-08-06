using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Tools.Unity.Lists.SelectableList<object>))]
public class ListSelectionCounter : MonoBehaviour
{
    #region Properties
    public Text Counter; 
    protected ISelectionCountable m_List;
    #endregion

    private void OnEnable()
    {
        m_List = GetComponent<ISelectionCountable>();
        Debug.Log(m_List);
        m_List.OnSelectionChanged.AddListener(() => Counter.text = m_List.NumberOfItemSelected.ToString());
    }
}
