using HBP.Data.Module3D;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Toolbar
{
    public abstract class Tool : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Lock to prevent the calls to the listeners when only changing the selected scene / column / view
        /// </summary>
        [HideInInspector] public bool ListenerLock;
        /// <summary>
        /// Reference to the selected scene
        /// </summary>
        public Base3DScene SelectedScene { protected get; set; }
        /// <summary>
        /// Reference to the selected column
        /// </summary>
        public Column3D SelectedColumn { protected get; set; }
        /// <summary>
        /// Reference to the selected view
        /// </summary>
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
        /// <summary>
        /// Get either the selected column or all columns
        /// </summary>
        /// <param name="isGlobal">Get all columns</param>
        /// <returns>List containing the selected column or all columns</returns>
        protected List<Column3D> GetColumnsDependingOnTypeAndGlobal(bool isGlobal)
        {
            List<Column3D> columns = new List<Column3D>();
            if (isGlobal)
            {
                if (SelectedColumn is Column3DAnatomy)
                {
                    columns.AddRange(SelectedScene.ColumnsAnatomy);
                }
                else if (SelectedColumn is Column3DIEEG)
                {
                    columns.AddRange(SelectedScene.ColumnsIEEG);
                }
                else if (SelectedColumn is Column3DCCEP)
                {
                    columns.AddRange(SelectedScene.ColumnsCCEP);
                }
                else if (SelectedColumn is Column3DFMRI)
                {
                    columns.AddRange(SelectedScene.ColumnsFMRI);
                }
                else if (SelectedColumn is Column3DMEG)
                {
                    columns.AddRange(SelectedScene.ColumnsMEG);
                }
            }
            else
            {
                columns.Add(SelectedColumn);
            }
            return columns;
        }
        #endregion
    }
}