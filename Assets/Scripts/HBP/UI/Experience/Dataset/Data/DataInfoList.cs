using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoList : Tools.Unity.Lists.SelectableListWithSave<HBP.Data.Experience.Dataset.DataInfo>
    {
        #region Attributs
        bool m_SortByName = false;
        bool m_SortByPatient = false;
        bool m_SortByMeasure = false;
        bool m_SortByEEG = false;
        bool m_SortByPOS = false;
        bool m_SortByProv = false;
        bool m_SortByState = false;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            if (m_SortByName)
            {
                m_Objects.OrderByDescending((x) => x.Name);
            }
            else
            {
                m_Objects.OrderBy((x) => x.Name);
            }
            m_SortByName = !m_SortByName;
            ApplySort();
        }
        public void SortByPatient()
        {
            if (m_SortByPatient)
            {
                m_Objects.OrderByDescending((x) => x.Patient.Name);
            }
            else
            {
                m_Objects.OrderBy((x) => x.Patient.Name);
            }
            m_SortByPatient = !m_SortByPatient;
            ApplySort();
        }
        public void SortByMeasure()
        {
            if (m_SortByMeasure)
            {
                m_Objects.OrderByDescending((x) => x.Protocol.Name);
            }
            else
            {
                m_Objects.OrderBy((x) => x.Protocol.Name);
            }
            m_SortByMeasure = !m_SortByMeasure;
            ApplySort();
        }
        public void SortByEEG()
        {
            if (m_SortByEEG)
            {
                m_Objects.OrderByDescending((x) => x.EEG);
            }
            else
            {
                m_Objects.OrderBy((x) => x.EEG);
            }
            m_SortByEEG = !m_SortByEEG;
            ApplySort();
        }
        public void SortByPOS()
        {
            if (m_SortByPOS)
            {
                m_Objects.OrderByDescending((x) => x.POS);
            }
            else
            {
                m_Objects.OrderBy((x) => x.POS);
            }
            m_SortByPOS = !m_SortByPOS;
            ApplySort();
        }
        public void SortByProv()
        {
            if (m_SortByProv)
            {
                m_Objects.OrderByDescending((x) => x.Protocol.Name);
            }
            else
            {
                m_Objects.OrderBy((x) => x.Protocol.Name);
            }
            m_SortByProv = !m_SortByProv;
            ApplySort();
        }
        public void SortByState()
        {
            if (m_SortByState)
            {
                m_Objects.OrderByDescending((x) => x.isOk);
            }
            else
            {
                m_Objects.OrderBy((x) => x.isOk);
            }
            m_SortByState = !m_SortByState;
            ApplySort();
        }
        #endregion
    }
}