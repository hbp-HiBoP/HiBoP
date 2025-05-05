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
        enum OrderBy { None, Name, DescendingName, Place, DescendingPlace, Date, DescendingDate }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] Button m_ResetFiltersButton;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_PlaceSortingDisplayer;
        [SerializeField] SortingDisplayer m_DateSortingDisplayer;
        #endregion

        #region Public Methods
        protected override void AddObject(Patient obj)
        {
            SortByNone();
            base.AddObject(obj);
        }

        public void OpenFilterWindow()
        {
            var filteringObjects = Objects.Select(o => (BaseData)o).ToList();
            if (filteringObjects.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No objects to filter", "The list you are trying to filter contains no object. This is not supported.").Forget();
                return;
            }

            var parentWindow = GetComponentInParent<Window>();

            var filterWindow = WindowsManager.Open("Filter window", parentWindow).GetComponent<ListFilter>();
            filterWindow.FilteringObjects = filteringObjects;
            filterWindow.SetPreset(PersistentDataManager.FilterConditionsPresets.CurrentPreset);
            filterWindow.OnApplyFilters.AddListener(mask =>
            {
                MaskList(mask, false);
                SortByNone();
            });

            if (parentWindow)
                parentWindow.WindowsReferencer.Add(filterWindow);
        }
        public void ResetFilters()
        {
            MaskList(Enumerable.Repeat(true, m_Objects.Count).ToArray(), false);
            SortByNone();
        }
        public override bool MaskList(bool[] mask, bool hide = true)
        {
            m_ResetFiltersButton.interactable = mask.Any(m => !m);
            return base.MaskList(mask, hide);
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
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        public void SortByPlace(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.Place;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Place).ToList();
                    m_OrderBy = OrderBy.DescendingPlace;
                    m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPlace()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPlace: SortByPlace(Sorting.Ascending); break;
                default: SortByPlace(Sorting.Descending); break;
            }
        }

        public void SortByDate(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.Date;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Date).ToList();
                    m_OrderBy = OrderBy.DescendingDate;
                    m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByDate()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingDate: SortByDate(Sorting.Ascending); break;
                default: SortByDate(Sorting.Descending); break;
            }
        }

        public void SortByNone()
        {
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PlaceSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_DateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_OrderBy = OrderBy.None;
        }
        #endregion

        #region Protected Methods
        protected override IEnumerable<Patient> DefaultSorting(IEnumerable<Patient> objects)
        {
            return objects.OrderBy(p => p.Place).ThenBy(p => p.Date).ThenBy(p => p.Name);
        }
        #endregion
    }
}