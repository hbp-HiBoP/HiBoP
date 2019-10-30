using HBP.Data.Experience.Dataset;
using Tools.Unity.Components;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoListGestion : ListGestion<DataInfo>
    {
        #region Properties
        [SerializeField] protected DataInfoList m_List;
        public override Tools.Unity.Lists.SelectableListWithItemAction<DataInfo> List => m_List;

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
        protected override ItemModifier<DataInfo> OpenModifier(DataInfo item, bool interactable)
        {
            DataInfoModifier modifier = base.OpenModifier(item, interactable) as DataInfoModifier;
            modifier.OnCanSave.AddListener(() => OnCanSaveModifier(modifier));
            return modifier;
        }
        protected void OnCanSaveModifier(DataInfoModifier modifier)
        {
            if(modifier.ItemTemp is PatientDataInfo patientDataInfo)
            {
                if (modifier.ItemTemp is iEEGDataInfo iEEGDataInfo)
                {
                    modifier.CanSave = !List.Objects.OfType<iEEGDataInfo>().Any(data => data.Patient == iEEGDataInfo.Patient && data.Name == iEEGDataInfo.Name && data != modifier.Item);
                }
                else if (modifier.ItemTemp is CCEPDataInfo ccepDataInfo)
                {
                    modifier.CanSave = !List.Objects.OfType<CCEPDataInfo>().Any(data => data.Patient == ccepDataInfo.Patient && data.Name == ccepDataInfo.Name && data.StimulatedChannel == ccepDataInfo.StimulatedChannel && data != modifier.Item);
                }
            }
            else
            {
                modifier.CanSave = !List.Objects.Any(data => data.Name == modifier.ItemTemp.Name && data != modifier.Item); // FIXME
            }
        }
        protected override void Add(DataInfo obj)
        {
            OnDataInfoNeedCheckErrors.Invoke(obj);
            base.Add(obj);
        }
        #endregion
    }
}