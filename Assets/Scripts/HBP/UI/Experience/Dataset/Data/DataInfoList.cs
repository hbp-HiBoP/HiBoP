using d = HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.SelectableListWithSave<d.DataInfo>
    {
        #region Attributs
        bool m_sortByName = false;
        bool m_sortByPatient = false;
        bool m_sortByMeasure = false;
        bool m_sortByEEG = false;
        bool m_sortByPOS = false;
        bool m_sortByProv = false;
        bool m_sortBySP = false;
        bool m_sortByMP = false;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_sortByName)
            {
                m_objects.OrderByDescending((x) => x.Name);
            }
            else
            {
                m_objects.OrderBy((x) => x.Name);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }
        public void SortByPatient()
        {
            if (m_sortByPatient)
            {
                m_objects.OrderByDescending((x) => x.Patient.Name);
            }
            else
            {
                m_objects.OrderBy((x) => x.Patient.Name);
            }
            m_sortByPatient = !m_sortByPatient;
            ApplySort();
        }
        public void SortByMeasure()
        {
            if (m_sortByMeasure)
            {
                m_objects.OrderByDescending((x) => x.Protocol.Name);
            }
            else
            {
                m_objects.OrderBy((x) => x.Protocol.Name);
            }
            m_sortByMeasure = !m_sortByMeasure;
            ApplySort();
        }
        public void SortByEEG()
        {
            if (m_sortByEEG)
            {
                m_objects.OrderByDescending((x) => x.EEG);
            }
            else
            {
                m_objects.OrderBy((x) => x.EEG);
            }
            m_sortByEEG = !m_sortByEEG;
            ApplySort();
        }
        public void SortByPOS()
        {
            if (m_sortByPOS)
            {
                m_objects.OrderByDescending((x) => x.POS);
            }
            else
            {
                m_objects.OrderBy((x) => x.POS);
            }
            m_sortByPOS = !m_sortByPOS;
            ApplySort();
        }
        public void SortByProv()
        {
            if (m_sortByProv)
            {
                m_objects.OrderByDescending((x) => x.Protocol.Name);
            }
            else
            {
                m_objects.OrderBy((x) => x.Protocol.Name);
            }
            m_sortByProv = !m_sortByProv;
            ApplySort();
        }
        public void SortBySP()
        {
            if (m_sortBySP)
            {
                m_objects.OrderByDescending((x) => x.UsableInSinglePatient);
            }
            else
            {
                m_objects.OrderBy((x) => x.UsableInSinglePatient);
            }
            m_sortBySP = !m_sortBySP;
            ApplySort();
        }
        public void SortByMP()
        {
            if (m_sortByMP)
            {
                m_objects.OrderByDescending((x) => x.UsableInMultiPatients);
            }
            else
            {
                m_objects.OrderBy((x) => x.UsableInMultiPatients);
            }
            m_sortByMP = !m_sortByMP;
            ApplySort();
        }
        #endregion
    }
}