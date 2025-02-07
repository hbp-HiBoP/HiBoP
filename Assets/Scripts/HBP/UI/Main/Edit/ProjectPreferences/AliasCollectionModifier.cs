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
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            base.OK();
            Object.SetAliases(m_AliasListGestion.List.Objects.ToList());
            PersistentDataManager.Aliases.Save();
            LoadingManager.Load(update => Dataset.CheckDatasetsAsync(DatabaseManager.Database.Protocols, update));
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_AliasListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        protected override void SetFields(AliasCollection objectToDisplay)
        {
            m_AliasListGestion.List.Set(objectToDisplay.Aliases);
        }
        #endregion
    }
}