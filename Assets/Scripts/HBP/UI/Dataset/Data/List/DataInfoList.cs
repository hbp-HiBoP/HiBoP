using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.ActionableList<Data.Experience.Dataset.DataInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patient, DescendingPatient, State, DescendingState, Type, DescendingType }
        OrderBy m_OrderBy = OrderBy.None;
        
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientSortingDisplayer;
        public SortingDisplayer m_TypeSortingDisplayer;
        public SortingDisplayer m_StateSortingDisplayer;

        public UnityEngine.Events.GenericEvent<Data.Experience.Dataset.DataInfo, int> OnUpdateObject = new UnityEngine.Events.GenericEvent<Data.Experience.Dataset.DataInfo, int>();
        #endregion

        #region Public Methods
        public override bool Add(Data.Experience.Dataset.DataInfo objectToAdd)
        {
            if (base.Add(objectToAdd))
            {
                SortByNone();
                return true;
            }
            else return false;
        }
        public override bool UpdateObject(Data.Experience.Dataset.DataInfo objectToUpdate)
        {
            int index = m_Objects.FindIndex(obj => obj == objectToUpdate);
            if (index != -1)
            {
                m_Objects[index] = objectToUpdate;
                OnUpdateObject.Invoke(objectToUpdate, index);
                if (GetItemFromObject(objectToUpdate, out Item<Data.Experience.Dataset.DataInfo> item))
                {
                    ActionnableItem<Data.Experience.Dataset.DataInfo> actionnableItem = item as ActionnableItem<Data.Experience.Dataset.DataInfo>;
                    actionnableItem.Object = objectToUpdate;
                    actionnableItem.OnChangeSelected.RemoveAllListeners();
                    actionnableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                    actionnableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
                    actionnableItem.OnAction.RemoveAllListeners();
                    actionnableItem.OnAction.AddListener((actionID) => m_OnAction.Invoke(objectToUpdate, actionID));
                    return true;
                }
            }
            return false;
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
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
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
            IEnumerable<Data.Experience.Dataset.DataInfo> patientDataInfo;
            IEnumerable<Data.Experience.Dataset.DataInfo> otherDataInfo;
            switch (sorting)
            {
                case Sorting.Ascending:
                    patientDataInfo = m_Objects.OfType<Data.Experience.Dataset.PatientDataInfo>().OrderByDescending((elt) => elt.Patient.Name);
                    otherDataInfo = m_Objects.Where(elt => !patientDataInfo.Contains(elt));
                    m_Objects = new System.Collections.Generic.List<Data.Experience.Dataset.DataInfo>(patientDataInfo);
                    m_Objects.AddRange(otherDataInfo);
                    m_OrderBy = OrderBy.Patient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    patientDataInfo = m_Objects.OfType<Data.Experience.Dataset.PatientDataInfo>().OrderBy((elt) => elt.Patient.Name);
                    otherDataInfo = m_Objects.Where(elt => !patientDataInfo.Contains(elt));
                    m_Objects = new System.Collections.Generic.List<Data.Experience.Dataset.DataInfo>(patientDataInfo);
                    m_Objects.AddRange(otherDataInfo);
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
                    m_Objects = m_Objects.OrderBy((elt) => elt.IsOk).ToList();
                    m_OrderBy = OrderBy.State;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.IsOk).ToList();
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
                    m_Objects = m_Objects.OrderBy((elt) => elt.GetType().ToString()).ToList();
                    m_OrderBy = OrderBy.Type;
                    m_TypeSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.GetType().ToString()).ToList();
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