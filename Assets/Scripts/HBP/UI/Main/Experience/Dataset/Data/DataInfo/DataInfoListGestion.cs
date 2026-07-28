using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;
using System;

namespace HBP.UI.Main
{
    public class DataInfoListGestion : ListGestion<Core.Data.DataInfo>
    {
        #region Properties
        [SerializeField] protected DataInfoList m_List;
        public override ActionableList<Core.Data.DataInfo> List => m_List;

        [SerializeField] protected DataInfoCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.DataInfo> ObjectCreator => m_ObjectCreator;
        #endregion

        #region Protected Methods
        protected override void OnSaveModifier(Core.Data.DataInfo obj)
        {
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
            obj.PendingValidationRequest = new Core.Data.ValidationRequest(
                Core.Data.ValidationAspect.DataInfoAll,
                dataInfoIDs: new[] { obj.ID },
                force: true);
            obj.MarkValidationStale(
                Core.Data.ValidationAspect.DataInfoAll);
            RenameObject(obj);
            if (!List.Objects.Contains(obj))
            {
                List.Add(obj);
            }
            else
            {
                List.UpdateObject(obj);
            }
            HasBeenModified = true;
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
            else if (obj is Core.Data.StaticDataInfo staticDataInfo)
            {
                IEnumerable<Core.Data.StaticDataInfo> staticDataInfos = List.Objects.OfType<Core.Data.StaticDataInfo>();
                if (staticDataInfos.Any(p => p.Name == obj.Name && p.Patient == staticDataInfo.Patient && !p.Equals(staticDataInfo)))
                {
                    int count = 1;
                    string name = string.Format("{0}({1})", obj.Name, count);
                    while (staticDataInfos.Any(p => p.Name == name && p.Patient == staticDataInfo.Patient && !p.Equals(staticDataInfo)))
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
