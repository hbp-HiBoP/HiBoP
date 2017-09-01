using UnityEngine;

namespace HBP.UI.Module3D.Tools
{
    public abstract class Tool : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Lock to prevent the calls to the listeners when only changing the selected scene / column / view
        /// </summary>
        [HideInInspector]
        public bool ListenerLock;
        #endregion

        #region Public Methods
        /// <summary>
        /// Add the listener to this tool
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public abstract void DefaultState();
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public abstract void UpdateInteractable();
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public virtual void UpdateStatus(Toolbar.UpdateToolbarType type)
        {

        }
        #endregion
    }
}