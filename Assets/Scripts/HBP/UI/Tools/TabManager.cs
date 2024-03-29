﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(ToggleGroup))]
    public class TabManager : MonoBehaviour
    {
        #region Properties
        public int MaxNbOfTabs = 8;
        public int MinNbOfTabs = 0;
        public int ActiveTabIndex
        {
            get
            {
                return m_Tabs.FindIndex(t => t.IsActive);
            }
            set
            {
                if (value >= 0 && value < m_Tabs.Count)
                {
                    m_Tabs[value].IsActive = true;
                }
            }
        }

        [SerializeField] GameObject TabPrefab;
        [SerializeField] Button AddButton, RemoveButton;

        public UnityEvent OnActiveTabChanged { get; } = new UnityEvent();

        public GenericEvent<int, int> OnSwapColumns { get; } = new GenericEvent<int, int>();

        List<Tab> m_Tabs = new List<Tab>();
        //Transform m_TabToMove;
        ToggleGroup m_ToggleGroup;
        #endregion

        #region Public Methods
        public void ChangeTabTitle(string title)
        {
            int position = ActiveTabIndex;
            if (position >= 0 && position < m_Tabs.Count)
            {
                m_Tabs[position].Title = title;
            }
        }
        public void AddTab(string titleTab = "", int position = -1, bool isActive = false)
        {
            Tab tab = Instantiate(TabPrefab, transform).GetComponent<Tab>();
            if (position > -1) m_Tabs.Insert(position, tab);
            else m_Tabs.Add(tab);

            tab.OnValueChanged.AddListener(OnTabChangeValue);
            tab.Title = titleTab;
            tab.Group = m_ToggleGroup;
            tab.IsActive = isActive;

            CheckIfButtonsHasToBeInteractable();
        }
        public void RemoveActiveTab()
        {
            RemoveTab(m_Tabs.FindIndex(tab => tab.IsActive));
        }
        public void RemoveTab(int position = -1)
        {
            Tab tabToRemove = m_Tabs[position];
            m_Tabs.Remove(tabToRemove);
            DestroyImmediate(tabToRemove.gameObject);
            if (m_Tabs.Count > 0)
            {
                m_Tabs[Mathf.Clamp(position, 0, m_Tabs.Count - 1)].IsActive = true;
            }
            else
            {
                OnActiveTabChanged.Invoke();
            }
            CheckIfButtonsHasToBeInteractable();
        }

        //public void OnBeginDrag(Transform tab)
        //{
        //    m_TabToMove = tab;
        //}
        //public void OnEndDrag(Transform tab)
        //{
        //    int i1 = m_TabToMove.GetSiblingIndex();
        //    int i2 = tab.GetSiblingIndex();
        //    m_TabToMove.SetSiblingIndex(i2);
        //    tab.SetSiblingIndex(i1);
        //    OnSwapColumns.Invoke(i1, i2);
        //}
        #endregion

        #region Private Methods
        void OnTabChangeValue(bool value)
        {
            if (value)
            {
                OnActiveTabChanged.Invoke();
            }
        }
        void Awake()
        {
            m_ToggleGroup = GetComponent<ToggleGroup>();
            CheckIfButtonsHasToBeInteractable();
        }
        void CheckIfButtonsHasToBeInteractable()
        {
            int nbOfChild = transform.childCount;
            AddButton.interactable = nbOfChild < MaxNbOfTabs;
            RemoveButton.interactable = nbOfChild > MinNbOfTabs;
        }
        #endregion
    }
}
