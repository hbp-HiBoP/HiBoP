using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.SelectableListWithSave<HBP.Data.Experience.Dataset.DataInfo>
    {
        #region Attributs
        enum OrderBy { None, Name, DescendingName, Patient, DescendingPatient, Measure, DescendingMeasure, EEG, DescendingEEG, POS, DescendingPOS, Prov, DescendingProv, State, DescendingState }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByPatient()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Patient:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Patient.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPatient;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Patient.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Patient;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByMeasure()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Measure:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Measure).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingMeasure;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Measure).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Measure;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByEEG()
        {
            switch (m_OrderBy)
            {
                case OrderBy.EEG:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.EEG).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingEEG;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.EEG).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.EEG;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByPOS()
        {
            switch (m_OrderBy)
            {
                case OrderBy.POS:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.POS).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPOS;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.POS).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.POS;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByProtocol()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Prov:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Protocol.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingProv;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Protocol.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Prov;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByState()
        {
            switch (m_OrderBy)
            {
                case OrderBy.State:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.isOk).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingState;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.isOk).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.State;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}