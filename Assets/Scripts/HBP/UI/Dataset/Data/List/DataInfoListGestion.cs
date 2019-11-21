using HBP.Data.Experience.Dataset;
using Tools.Unity.Components;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoListGestion : ListGestion<DataInfo>
    {
        #region Properties
        [SerializeField] protected DataInfoList m_List;
        public override Tools.Unity.Lists.ActionableList<DataInfo> List => m_List;

        [SerializeField] protected DataInfoCreator m_ObjectCreator;
        public override ObjectCreator<DataInfo> ObjectCreator => m_ObjectCreator;

        public GenericEvent<DataInfo> OnDataInfoNeedCheckErrors { get; } = new GenericEvent<DataInfo>();
        #endregion

        #region Public Methods
        public void UpdateAllObjects()
        {
            foreach (var obj in List.Objects)
            {
                List.UpdateObject(obj);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(DataInfo obj)
        {
            OnDataInfoNeedCheckErrors.Invoke(obj);
            if(obj is iEEGDataInfo ieegDataInfo)
            {
                IEnumerable<iEEGDataInfo> ieegDataInfos = List.Objects.OfType<iEEGDataInfo>();
                if(ieegDataInfos.Any(p => p.Name == obj.Name && p.Patient == ieegDataInfo.Patient && !p.Equals(ieegDataInfo)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (ieegDataInfos.Any(p => p.Name == name && p.Patient == ieegDataInfo.Patient && !p.Equals(ieegDataInfo)))
                    {
                        count++;
                        name = string.Format("{0}({1})", obj.Name, count);
                    }
                    obj.Name = name;
                }
            }
            else if(obj is CCEPDataInfo ccepDataInfo)
            {
                IEnumerable<CCEPDataInfo> ccepDataInfos = List.Objects.OfType<CCEPDataInfo>();
                if (ccepDataInfos.Any(p => p.Name == obj.Name && p.Patient == ccepDataInfo.Patient && p.StimulatedChannel == ccepDataInfo.StimulatedChannel && !p.Equals(ccepDataInfo)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (ccepDataInfos.Any(p => p.Name == name && p.Patient == ccepDataInfo.Patient && p.StimulatedChannel == ccepDataInfo.StimulatedChannel && !p.Equals(ccepDataInfo)))
                    {
                        count++;
                        name = string.Format("{0}({1})", obj.Name, count);
                    }
                    obj.Name = name;
                }
            }
            else
            {
                if (m_List.Objects.Any(p => p.GetType() == obj.GetType() && p.Name == obj.Name && !p.Equals(obj)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (m_List.Objects.Any(p => p.Name == name))
                    {
                        count++;
                        name = string.Format("{0}({1})", obj.Name, count);
                    }
                    obj.Name = name;
                }
            }
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        #endregion
    }
}