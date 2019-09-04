using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D.Tools
{
    public abstract class Tool : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Lock to prevent the calls to the listeners when only changing the selected scene / column / view
        /// </summary>
        [HideInInspector] public bool ListenerLock;
        public Base3DScene SelectedScene { protected get; set; }
        public Column3D SelectedColumn { protected get; set; }
        public View3D SelectedView { protected get; set; }
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
        public virtual void UpdateStatus()
        {

        }
        /// <summary>
        /// Update the tool
        /// </summary>
        public void UpdateTool()
        {
            if (SelectedScene != null && SelectedColumn != null && SelectedView != null)
            {
                UpdateInteractable();
                UpdateStatus();
            }
            else
            {
                DefaultState();
            }
        }
        #endregion

        #region Private Methods
        protected List<Column3DDynamic> GetColumnsDependingOnTypeAndGlobal(bool isGlobal)
        {
            List<Column3DDynamic> columns = new List<Column3DDynamic>();
            if (isGlobal)
            {
                if (SelectedColumn is Column3DIEEG)
                {
                    columns.AddRange(SelectedScene.ColumnManager.ColumnsIEEG);
                }
                else if (SelectedColumn is Column3DCCEP)
                {
                    columns.AddRange(SelectedScene.ColumnManager.ColumnsCCEP);
                }
            }
            else
            {
                columns.Add((Column3DDynamic)SelectedColumn);
            }
            return columns;
        }
        #endregion
    }
}