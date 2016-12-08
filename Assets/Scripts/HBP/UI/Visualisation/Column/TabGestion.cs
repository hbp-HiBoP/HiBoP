using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HBP.UI
{
    public class SwapColumnsEvent : UnityEvent<int, int> {}

    public class TabGestion : MonoBehaviour
    {
        #region Properties
        UnityEvent onChangeSelectedTabEvent = new UnityEvent();
        public UnityEvent OnChangeSelectedTabEvent
        {
            get { return onChangeSelectedTabEvent; }
        }
        SwapColumnsEvent onSwapColumnsEvent = new SwapColumnsEvent();
        public SwapColumnsEvent OnSwapColumnsEvent
        {
            get { return onSwapColumnsEvent; }
        }

        public ToggleGroup ToggleGroup
        {
            get { return GetComponent<ToggleGroup>(); }
        }

        [SerializeField]
        private GameObject tabPrefab;
        private Transform tabToMove;
        #endregion

        #region Public Methods
        public void Clear()
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Toggle>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        public void AddTab()
        {
            GameObject l_tab = Instantiate(tabPrefab) as GameObject;
            l_tab.transform.SetParent(transform);
            l_tab.transform.localScale = new Vector3(1, 1, 1);
            l_tab.transform.localPosition = new Vector3(0, 0, 0);
            l_tab.transform.SetAsLastSibling();
            l_tab.transform.SetSiblingIndex(l_tab.transform.GetSiblingIndex() - 1);
            l_tab.GetComponent<Toggle>().group = ToggleGroup;
            l_tab.GetComponent<Toggle>().isOn = true;
            if (transform.childCount == 4)
            {
                transform.GetChild(0).GetComponent<Button>().interactable = true;
            }
            else if (transform.childCount == 10)
            {
                transform.GetChild(transform.childCount - 1).GetComponent<Button>().interactable = false;
            }
        }
        public void RemoveTab()
        {
            List<Toggle> l_toggle = new List<Toggle>(ToggleGroup.ActiveToggles());
            int index = l_toggle[0].transform.GetSiblingIndex();
            if (l_toggle.Count > 0)
            {
                DestroyImmediate(l_toggle[0].gameObject);
            }
            if (transform.childCount == 3)
            {
                transform.GetChild(0).GetComponent<Button>().interactable = false;
            }
            else if (transform.childCount == 9)
            {
                transform.GetChild(transform.childCount).GetComponent<Button>().interactable = true;
            }
            Tab[] l_tabs = GetComponentsInChildren<Tab>();
            if (index - 2 < 0) index = 2;
            l_tabs[index - 2].IsOn = true;
        }
        public void OnBeginDrag(Transform tab)
        {
            tabToMove = tab;
        }
        public void OnEndDrag(Transform tab)
        {
            int i1 = tabToMove.GetSiblingIndex();
            int i2 = tab.GetSiblingIndex();
            tabToMove.SetSiblingIndex(i2);
            tab.SetSiblingIndex(i1);
            OnSwapColumnsEvent.Invoke(i1, i2);
        }
        public void OnChangeSelectedTab()
        {
            OnChangeSelectedTabEvent.Invoke();
        }
        #endregion
    }
}
