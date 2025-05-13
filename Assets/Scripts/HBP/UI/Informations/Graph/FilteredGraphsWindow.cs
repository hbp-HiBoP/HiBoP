using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class FilteredGraphsWindow : Window
    {
        [SerializeField] private GameObject m_FilteredGraphPrefabItem;
        [SerializeField] private Transform m_ParentFilteredGraphItem;

        private List<ChannelStruct[]> m_FilteredChannelStructs;
        public List<ChannelStruct[]> FilteredChannelStructs
        {
            get => m_FilteredChannelStructs;
            set
            {
                m_FilteredChannelStructs = value;
                DisplayItems();
            }
        }

        public GenericEvent<ChannelStruct[]> OnRemoveChannelStructs = new();

        private void DisplayItems()
        {
            foreach (var fcs in m_FilteredChannelStructs)
            {
                GameObject obj = Instantiate(m_FilteredGraphPrefabItem, m_ParentFilteredGraphItem);
                Dictionary<Patient, List<string>> channelByPatient = new();
                foreach (var channelStruct in fcs)
                {
                    if (!channelByPatient.ContainsKey(channelStruct.Patient))
                    {
                        channelByPatient[channelStruct.Patient] = new List<string>();
                    }
                    channelByPatient[channelStruct.Patient].Add(channelStruct.Channel);
                }
                List<string> names = channelByPatient.Select(kvp => $"{kvp.Key.Name}_{kvp.Key.Date}_{string.Join("_", kvp.Value)}").ToList();
                obj.GetComponentInChildren<Text>().text = $"{string.Join("-", names)}";
                obj.GetComponentInChildren<Button>().onClick.AddSafeListener(() =>
                {
                    OnRemoveChannelStructs.Invoke(fcs);
                    Destroy(obj);
                }, gameObject);
            }
        }
    }
}