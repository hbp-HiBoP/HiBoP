using System;
using Tools.Unity.Components;

namespace HBP.UI
{
    public abstract class GestionWindow<T> : DialogWindow where T : ICloneable, ICopiable, new()
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
                ListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            ListGestion.WindowsReferencer.OnOpenWindow.AddListener(window => WindowsReferencer.Add(window));
        }
        #endregion
    }
}