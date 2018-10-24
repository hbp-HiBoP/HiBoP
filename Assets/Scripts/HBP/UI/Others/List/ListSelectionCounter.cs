using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(Lists.SelectableList<object>))]
    public class ListSelectionCounter : MonoBehaviour
    {
        #region Properties
        public Text Counter;
        protected ISelectionCountable m_List;
        #endregion

        private void OnEnable()
        {
            m_List = GetComponent<ISelectionCountable>();
            m_List.OnSelectionChanged.AddListener(() => Counter.text = m_List.NumberOfItemSelected.ToString());
        }
    }
}