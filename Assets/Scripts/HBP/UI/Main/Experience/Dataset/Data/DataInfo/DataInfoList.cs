using HBP.Core.Preferences;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    /// <summary>
    /// List to display DataInfos.
    /// </summary>
    public class DataInfoList : ActionableList<Core.Data.DataInfo>
    {
        #region Properties

        enum OrderBy
        {
            None,
            Name,
            DescendingName,
            Patient,
            DescendingPatient,
            State,
            DescendingState,
            Type,
            DescendingType
        }

        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] Button m_ResetFiltersButton;
        [SerializeField] Toggle m_ShowFilteredObjectsToggle;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientSortingDisplayer;
        public SortingDisplayer m_TypeSortingDisplayer;
        public SortingDisplayer m_StateSortingDisplayer;

        #endregion

        #region Public Methods

        /// <summary>
        /// Add dataInfo.
        /// </summary>
        /// <param name="objectToAdd">DataInfo to add</param>
        /// <returns>True if end without errors, False otherwise</returns>
        protected override void AddObject(Core.Data.DataInfo objectToAdd)
        {
            SortByNone();
            base.AddObject(objectToAdd);
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

        #endregion

        #region Sorting Methods

        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
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
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by patient.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByPatient(Sorting sorting)
        {
            System.Collections.Generic.List<Core.Data.PatientDataInfo> patientDataInfo = new();
            System.Collections.Generic.List<Core.Data.DataInfo> otherDataInfo = new();
            foreach (var data in m_DisplayedObjects)
            {
                if (data is Core.Data.PatientDataInfo patientData) patientDataInfo.Add(patientData);
                else otherDataInfo.Add(data);
            }

            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = new System.Collections.Generic.List<Core.Data.DataInfo>(patientDataInfo.OrderByDescending((elt) => elt.Patient.Name));
                    m_DisplayedObjects.AddRange(otherDataInfo);
                    m_OrderBy = OrderBy.Patient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = new System.Collections.Generic.List<Core.Data.DataInfo>(patientDataInfo.OrderBy((elt) => elt.Patient.Name));
                    m_DisplayedObjects.AddRange(otherDataInfo);
                    m_OrderBy = OrderBy.DescendingPatient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by patient.
        /// </summary>
        public void SortByPatient()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatient: SortByPatient(Sorting.Ascending); break;
                default: SortByPatient(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by state.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByState(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.State).ToList();
                    m_OrderBy = OrderBy.State;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.State).ToList();
                    m_OrderBy = OrderBy.DescendingState;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by sate.
        /// </summary>
        public void SortByState()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingState: SortByState(Sorting.Ascending); break;
                default: SortByState(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by state.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByType(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.DescendingType;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        /// <summary>
        /// Sort by sate.
        /// </summary>
        public void SortByType()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingType: SortByType(Sorting.Ascending); break;
                default: SortByType(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;

            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        #endregion
    }
}
