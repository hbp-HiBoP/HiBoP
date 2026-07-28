using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class ChannelStructList : SelectableList<ChannelStruct>
    {
        #region Properties

        enum OrderBy
        {
            None,
            Channel,
            DescendingChannel,
            Patient,
            DescendingPatient
        }

        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_ChannelSortingDisplayer;
        [SerializeField] SortingDisplayer m_PatientSortingDisplayer;

        #endregion

        #region Private Methods

        protected override void AddObject(ChannelStruct objectToAdd)
        {
            base.AddObject(objectToAdd);
            switch (m_OrderBy)
            {
                case OrderBy.None:
                    SortByNone();
                    break;
                case OrderBy.Channel:
                    SortByChannel(Sorting.Ascending);
                    break;
                case OrderBy.DescendingChannel:
                    SortByChannel(Sorting.Descending);
                    break;
                case OrderBy.Patient:
                    SortByPatient(Sorting.Ascending);
                    break;
                case OrderBy.DescendingPatient:
                    SortByPatient(Sorting.Descending);
                    break;
                default:
                    SortByNone();
                    break;
            }
        }

        #endregion

        #region Public Methods

        public void SelectFilteredSites()
        {
            var filteredSites = Module3DMain.SelectedScene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked && s.State.IsFiltered).Select(s => new ChannelStruct(s));
            DeselectAll();
            Select(filteredSites);
        }

        #endregion

        #region Sorting Methods

        public void SortByChannel(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending(c => c.Channel, new SiteNameComparer()).ToList();
                    m_OrderBy = OrderBy.Channel;
                    m_ChannelSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy(c => c.Channel, new SiteNameComparer()).ToList();
                    m_OrderBy = OrderBy.DescendingChannel;
                    m_ChannelSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        public void SortByChannel()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingChannel: SortByChannel(Sorting.Ascending); break;
                default: SortByChannel(Sorting.Descending); break;
            }
        }

        public void SortByPatient(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderByDescending((elt) => elt.Patient.Name).ThenBy(c => c.Channel, new SiteNameComparer()).ToList();
                    m_OrderBy = OrderBy.Patient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_DisplayedObjects = m_DisplayedObjects.OrderBy((elt) => elt.Patient.Name).ThenBy(c => c.Channel, new SiteNameComparer()).ToList();
                    m_OrderBy = OrderBy.DescendingPatient;
                    m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }

            Refresh();
            m_ChannelSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        public void SortByPatient()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingPatient: SortByPatient(Sorting.Ascending); break;
                default: SortByPatient(Sorting.Descending); break;
            }
        }

        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_ChannelSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_PatientSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

        #endregion
    }
}
