using HBP.Core.Data;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabasePatientList : SelectableList<Patient>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] Button m_ResetFiltersButton;
        [SerializeField] Toggle m_ShowFilteredObjectsToggle;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        #endregion

        #region Public Methods
        protected override void AddObject(Patient obj)
        {
            SortByNone();
            base.AddObject(obj);
        }

        public void OpenFilterWindow()
        {
            var filteringObjects = Objects.Select(o => (object)o).ToList();
            if (filteringObjects.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No objects to filter", "The list you are trying to filter contains no object. This is not supported.").Forget();
                return;
            }

            var parentWindow = GetComponentInParent<Window>();

            var filterWindow = WindowsManager.Open("Filter window", parentWindow).GetComponent<ListFilter>();
            filterWindow.FilteringObjects = filteringObjects;
            filterWindow.SetPreset(PersistentDataManager.FilterConditionsPresets.GetCurrentPreset(filteringObjects[0].GetType()));
            filterWindow.OnApplyFilters.AddListener(mask =>
            {
                MaskList(mask, !m_ShowFilteredObjectsToggle.isOn);
                SortByNone();
            });

            if (parentWindow)
                parentWindow.WindowsReferencer.Add(filterWindow);
        }
        public void ResetFilters()
        {
            MaskList(Enumerable.Repeat(true, m_Objects.Count).ToArray(), !m_ShowFilteredObjectsToggle.isOn);
            SortByNone();
        }
        public override bool MaskList(bool[] mask, bool hide = true)
        {
            bool hasFilteredObjects = mask.Any(m => !m);
            m_ResetFiltersButton.interactable = hasFilteredObjects;
            m_ShowFilteredObjectsToggle.interactable = hasFilteredObjects;
            if (hasFilteredObjects) m_ShowFilteredObjectsToggle.isOn = !hide;
            
            return base.MaskList(mask, hide);
        }
        public void OnShowFilteredObjectsToggleChanged(bool showFiltered)
        {
            HideMaskedObjects = !showFiltered;
        }

        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion

        #region Protected Methods
        protected override IEnumerable<Patient> DefaultSorting(IEnumerable<Patient> objects)
        {
            return objects.OrderBy(p => p.Name);
        }
        #endregion
    }
}