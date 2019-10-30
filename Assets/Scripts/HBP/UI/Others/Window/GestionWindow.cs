using System;
using Tools.Unity.Components;

namespace HBP.UI
{
    public abstract class GestionWindow<T> : SavableWindow where T : ICloneable, ICopiable, new()
    {
        #region Properties
        public abstract ListGestion<T> ListGestion { get; }

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                ListGestion.Interactable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            ListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
        }
        #endregion
    }
}