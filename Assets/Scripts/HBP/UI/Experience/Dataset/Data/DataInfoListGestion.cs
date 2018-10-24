using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using Tools.Unity.Components;
using UnityEngine;
using System.Linq;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoListGestion : ListGestion<DataInfo>
    {
        #region Properties
        [SerializeField] new DataInfoList List;
        public override List<DataInfo> Items
        {
            get
            {
                return base.Items;
            }

            set
            {
                List.Initialize();
                base.Items = value;
                List.SortByName(DataInfoList.Sorting.Descending);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        #endregion


        #region Propected Methods
        protected override void OpenModifier(DataInfo item, bool interactable)
        {
            DataInfoModifier modifier =(DataInfoModifier) ApplicationState.WindowsManager.OpenModifier(item, interactable);
            modifier.OnClose.AddListener(() => OnCloseModifier(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            modifier.OnCanSave.AddListener(() => OnCanSaveModifier(modifier));
            OnOpenSavableWindow.Invoke(modifier);
            Modifiers.Add(modifier);
        }
        protected void OnCanSaveModifier(DataInfoModifier modifier)
        {
            modifier.CanSave = !Items.Any(data => data.Patient == modifier.ItemTemp.Patient && data.Name == modifier.ItemTemp.Name && data != modifier.Item);
        }
        #endregion
    }
}