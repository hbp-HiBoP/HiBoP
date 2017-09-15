using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.SelectableListWithSave<HBP.Data.Experience.Dataset.DataInfo>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Patient, DescendingPatient, Measure, DescendingMeasure, EEG, DescendingEEG, POS, DescendingPOS, Prov, DescendingProv, State, DescendingState }
        OrderBy m_OrderBy = OrderBy.None;

        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_PatientSortingDisplayer;
        public SortingDisplayer m_MeasureSortingDisplayer;
        public SortingDisplayer m_EEGSortingDisplayer;
        public SortingDisplayer m_POSSortingDisplayer;
        public SortingDisplayer m_ProvSortingDisplayer;
        public SortingDisplayer m_StateSortingDisplayer;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPatient()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatient:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patient.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;               
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patient.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByMeasure()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMeasure:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Measure).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Measure;
                    m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Measure).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMeasure;
                    m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByEEG()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingEEG:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.EEG).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.EEG;
                    m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.EEG).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingEEG;
                    m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByPOS()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPOS:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.POS).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.POS;
                    m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.POS).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPOS;
                    m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByProtocol()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingProv:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Protocol.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Prov;
                    m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Protocol.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingProv;
                    m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        public void SortByState()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingState:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.isOk).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.State;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.isOk).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingState;
                    m_StateSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeasureSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_EEGSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_POSSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_ProvSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}