using System.Linq;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Data;
using HBP.Data.Preferences;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Database;
using System;

namespace HBP.UI.Main
{
    public class AliasCollectionModifier : ObjectModifier<AliasCollection>
    {
        #region Properties
        [SerializeField] AliasListGestion m_AliasListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_AliasListGestion.Interactable = value;
                m_AliasListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            bool requiresCheck = false;

            if (m_AliasListGestion.HasBeenModified)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Data check required", "Some aliases have been modified. A data integrity check is required to ensure there are no errors.\n\nWould you like to proceed with the check?", "Check", "Cancel");

                if (result == 0)
                    requiresCheck = true;
                else
                    return;
            }

            base.OK();
            Object.SetAliases(m_AliasListGestion.List.Objects.ToList());
            PersistentDataManager.Aliases.Save();

            if (requiresCheck)
                LoadingManager.Load(update => Dataset.CheckDatasetsAsync(DatabaseManager.Database.Protocols, true, update));
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_AliasListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
        }
        protected override void SetFields(AliasCollection objectToDisplay)
        {
            m_AliasListGestion.List.Set(objectToDisplay.Aliases);
        }
        #endregion
    }
}