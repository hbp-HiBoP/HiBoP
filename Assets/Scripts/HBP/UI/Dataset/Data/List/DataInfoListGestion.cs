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
            DataInfo[] dataInfos = List.Objects.ToArray();
            foreach (var obj in dataInfos)
            {
                List.UpdateObject(obj);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(DataInfo obj)
        {
            OnDataInfoNeedCheckErrors.Invoke(obj);
            RenameObject(obj);
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        protected override void OnObjectCreated(DataInfo obj)
        {
            OnDataInfoNeedCheckErrors.Invoke(obj);
            RenameObject(obj);
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
        }
        private void RenameObject(DataInfo obj)
        {
            if (obj is IEEGDataInfo ieegDataInfo)
            {
                IEnumerable<IEEGDataInfo> ieegDataInfos = List.Objects.OfType<IEEGDataInfo>();
                if (ieegDataInfos.Any(p => p.Name == obj.Name && p.Patient == ieegDataInfo.Patient && !p.Equals(ieegDataInfo)))
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
            else if (obj is CCEPDataInfo ccepDataInfo)
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
            else if (obj is FMRIDataInfo fmriDataInfo)
            {
                IEnumerable<FMRIDataInfo> fmriDataInfos = List.Objects.OfType<FMRIDataInfo>();
                if (fmriDataInfos.Any(p => p.Name == obj.Name && p.Patient == fmriDataInfo.Patient && !p.Equals(fmriDataInfo)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (fmriDataInfos.Any(p => p.Name == name && p.Patient == fmriDataInfo.Patient && !p.Equals(fmriDataInfo)))
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
        }
        #endregion
    }
}