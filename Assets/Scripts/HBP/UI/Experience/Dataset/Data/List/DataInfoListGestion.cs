using System.Collections.Generic;
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
        [SerializeField] new DataInfoList List;
        public override List<DataInfo> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(DataInfoList.Sorting.Descending);
            }
        }

        public GenericEvent<DataInfo> OnDataInfoNeedCheckErrors { get; } = new GenericEvent<DataInfo>();
        public GenericEvent<DataInfo> OnAddDataInfo { get; } = new GenericEvent<DataInfo>();
        public GenericEvent<DataInfo> OnRemoveDataInfo { get; } = new GenericEvent<DataInfo>();
        public GenericEvent<DataInfo> OnUpdateDataInfo { get; } = new GenericEvent<DataInfo>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            List.OnUpdateObject.AddListener((dataInfo, index) => m_Objects[index] = dataInfo);
            base.List = List;
            base.Initialize();
        }
        public void UpdateAllObjects()
        {
            List<DataInfo> objects = m_Objects.ToList();
            foreach (var _object in objects)
            {
                List.UpdateObject(_object);
            }
        }
        public override void Remove(DataInfo item)
        {
            base.Remove(item);
            OnRemoveDataInfo.Invoke(item);
        }
        #endregion

        #region Protected Methods
        protected override ItemModifier<DataInfo> OpenModifier(DataInfo item, bool interactable)
        {
            DataInfoModifier modifier =(DataInfoModifier) ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseSubWindow(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            modifier.OnCanSave.AddListener(() => OnCanSaveModifier(modifier));
            OnOpenSavableWindow.Invoke(modifier);
            SubWindows.Add(modifier);
            return modifier;
        }
        protected void OnCanSaveModifier(DataInfoModifier modifier)
        {
            if(modifier.ItemTemp is PatientDataInfo patientDataInfo)
            {
                if (modifier.ItemTemp is iEEGDataInfo iEEGDataInfo)
                {
                    modifier.CanSave = !Objects.OfType<iEEGDataInfo>().Any(data => data.Patient == iEEGDataInfo.Patient && data.Name == iEEGDataInfo.Name && data != modifier.Item);
                }
                else if (modifier.ItemTemp is CCEPDataInfo ccepDataInfo)
                {
                    modifier.CanSave = !Objects.OfType<CCEPDataInfo>().Any(data => data.Patient == ccepDataInfo.Patient && data.Name == ccepDataInfo.Name && data.StimulatedChannel == ccepDataInfo.StimulatedChannel && data != modifier.Item);
                }
            }
            else
            {
                modifier.CanSave = !Objects.Any(data => data.Name == modifier.ItemTemp.Name && data != modifier.Item); // FIXME
            }
        }
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            DataInfo item = new iEEGDataInfo();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item, Interactable);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(Objects.ToArray());
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item, Interactable);
                    }
                    break;
            }
        }
        protected override void OnSaveModifier(ItemModifier<DataInfo> modifier)
        {
            OnDataInfoNeedCheckErrors.Invoke(modifier.Item);
            if (!Objects.Contains(modifier.Item))
            {
                Add(modifier.Item);
                OnAddDataInfo.Invoke(modifier.Item);
            }
            else
            {
                UpdateItem(modifier.Item);
                OnUpdateDataInfo.Invoke(modifier.Item);
            }
            OnCloseSavableWindow.Invoke(modifier);
            SubWindows.Remove(modifier);
        }
        #endregion
    }
}