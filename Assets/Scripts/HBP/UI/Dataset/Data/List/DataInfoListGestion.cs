using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoListGestion : ListGestion<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] protected DataInfoList m_List;
        public override Tools.Unity.Lists.ActionableList<Core.Data.DataInfo> List => m_List;

        [SerializeField] protected DataInfoCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.DataInfo> ObjectCreator => m_ObjectCreator;

        public GenericEvent<Core.Data.DataInfo> OnDataInfoNeedCheckErrors { get; } = new GenericEvent<Core.Data.DataInfo>();
        #endregion

        #region Public Methods
        public void UpdateAllObjects()
        {
            Core.Data.DataInfo[] dataInfos = List.Objects.ToArray();
            foreach (var obj in dataInfos)
            {
                List.UpdateObject(obj);
            }
        }
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(Core.Data.DataInfo obj)
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
        protected override void OnObjectCreated(Core.Data.DataInfo obj)
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
        private void RenameObject(Core.Data.DataInfo obj)
        {
            if (obj is Core.Data.IEEGDataInfo ieegDataInfo)
            {
                IEnumerable<Core.Data.IEEGDataInfo> ieegDataInfos = List.Objects.OfType<Core.Data.IEEGDataInfo>();
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
            else if (obj is Core.Data.CCEPDataInfo ccepDataInfo)
            {
                IEnumerable<Core.Data.CCEPDataInfo> ccepDataInfos = List.Objects.OfType<Core.Data.CCEPDataInfo>();
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
            else if (obj is Core.Data.FMRIDataInfo fmriDataInfo)
            {
                IEnumerable<Core.Data.FMRIDataInfo> fmriDataInfos = List.Objects.OfType<Core.Data.FMRIDataInfo>();
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