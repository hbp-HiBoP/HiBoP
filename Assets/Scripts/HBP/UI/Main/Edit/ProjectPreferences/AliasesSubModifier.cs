using System.Linq;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class AliasesSubModifier : SubModifier<Core.Data.ProjectPreferences>
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
        public override void Save()
        {
            base.Save();
            Object.Aliases = m_AliasListGestion.List.Objects.ToList();
        }
        public override void Initialize()
        {
            base.Initialize();
            m_AliasListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_AliasListGestion.List.Set(objectToDisplay.Aliases);
        }
        #endregion
    }
}