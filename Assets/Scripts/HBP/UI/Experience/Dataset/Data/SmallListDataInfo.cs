using UnityEngine;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class SmallListDataInfo : MonoBehaviour
    {
        #region Attributs
        [SerializeField]
        private GameObject m_panel;
        private List<DataInfoWithDatasetLabel> m_dataInfoWithDatasetLabel;
        #endregion

        #region Public Methods
        public void Display(DataInfoWithDatasetLabel[] dataInfoWithDatasetLabel)
        {
            List<DataInfoWithDatasetLabel> l_exp = new List<DataInfoWithDatasetLabel>(dataInfoWithDatasetLabel);
            List<DataInfoWithDatasetLabel> l_expToAdd = new List<DataInfoWithDatasetLabel>();
            List<DataInfoWithDatasetLabel> l_expToRemove = new List<DataInfoWithDatasetLabel>();
            foreach (DataInfoWithDatasetLabel exp in m_dataInfoWithDatasetLabel)
            {
                if (!l_exp.Contains(exp))
                {
                    l_expToRemove.Add(exp);
                }
            }
            foreach (DataInfoWithDatasetLabel exp in dataInfoWithDatasetLabel)
            {
                if (!m_dataInfoWithDatasetLabel.Contains(exp))
                {
                    l_expToAdd.Add(exp);
                }
            }
            Remove(l_expToRemove.ToArray());
            Add(l_expToAdd.ToArray());
        }

        public void Add(DataInfoWithDatasetLabel[] dataInfoWithDatasetLabel)
        {
            foreach (DataInfoWithDatasetLabel i_dataInfoWithDatasetLabel in dataInfoWithDatasetLabel)
            {
                Add(i_dataInfoWithDatasetLabel);
            }
        }

        public void Add(DataInfoWithDatasetLabel dataInfoWinthDatasetLabel)
        {
            GameObject l_panel = Instantiate(m_panel);
            l_panel.name = dataInfoWinthDatasetLabel.DatasetLabel + " => " + dataInfoWinthDatasetLabel.DataInfo.Name;
            l_panel.transform.SetParent(transform);
            l_panel.GetComponent<SmallListItemDataInfo>().Set(dataInfoWinthDatasetLabel.DatasetLabel, dataInfoWinthDatasetLabel.DataInfo);
            m_dataInfoWithDatasetLabel.Add(dataInfoWinthDatasetLabel);
        }

        public void Remove(DataInfoWithDatasetLabel[] experienceDataToRemove)
        {
            foreach (DataInfoWithDatasetLabel exp in experienceDataToRemove)
            {
                Remove(exp);
            }
        }

        public void Remove(DataInfoWithDatasetLabel experienceDataToRemove)
        {
            int length = m_dataInfoWithDatasetLabel.Count;
            for (int i = 0; i < length; i++)
            {
                if (experienceDataToRemove.DataInfo == m_dataInfoWithDatasetLabel[i].DataInfo)
                {
                    Destroy(transform.GetChild(i).gameObject);
                    m_dataInfoWithDatasetLabel.RemoveAt(i);
                }
            }
        }
        #endregion
    }
}
