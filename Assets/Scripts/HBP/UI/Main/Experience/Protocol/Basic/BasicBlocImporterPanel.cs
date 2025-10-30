using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Main
{
    public abstract class BasicBlocImporterPanel : MonoBehaviour
    {
        #region Properties
        protected BlocImporterData m_Data;
        #endregion

        #region Events
        [HideInInspector] public UnityEvent OnUpdateNavigation = new UnityEvent();
        #endregion

        #region Public Methods
        public virtual void Initialize(BlocImporterData data)
        {
            m_Data = data;
        }
        public abstract bool CanProceed();
        public abstract void OnProceed();
        public abstract void Refresh();
        #endregion
    }
}