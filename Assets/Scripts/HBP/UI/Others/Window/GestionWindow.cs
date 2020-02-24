using System;
using Tools.Unity.Components;

namespace HBP.UI
{
    /// <summary>
    /// Abstract generic class for every gestion window. A gestion window is a window to modify a list of elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GestionWindow<T> : DialogWindow where T : ICloneable, ICopiable, new()
    {
        #region Properties
        /// <summary>
        /// Class which manage the list of elements.
        /// </summary>
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