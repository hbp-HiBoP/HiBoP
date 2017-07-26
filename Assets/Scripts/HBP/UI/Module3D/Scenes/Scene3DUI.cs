﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    public class Scene3DUI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Associated logical Base3DScene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Linked resizable grid
        /// </summary>
        private ResizableGrid m_ResizableGrid;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_ResizableGrid = GetComponent<ResizableGrid>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the UI Scene
        /// </summary>
        /// <param name="scene">Logical associated scene</param>
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            for (int i = 0; i < m_Scene.ColumnManager.Columns.Count; i++)
            {
                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns[i]);
            }
            m_ResizableGrid.AddViewLine();
            for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
            {
                m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns[i], m_Scene.ColumnManager.Columns[i].Views.Last());
            }

            // Listeners
            m_Scene.ColumnManager.OnAddColumn.AddListener(() =>
            {
                if (!m_Scene) return; // if the scene has not been initialized, don't proceed

                m_ResizableGrid.AddColumn();
                m_ResizableGrid.Columns.Last().GetComponent<Column3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns.Last());
            });
            m_Scene.ColumnManager.OnRemoveColumn.AddListener((column) =>
            {
                if (!m_Scene) return;

                int columnID = m_Scene.ColumnManager.Columns.ToList().FindIndex((c) => c == column);
                m_ResizableGrid.RemoveColumn(m_ResizableGrid.Columns[columnID]);
            });
            m_Scene.ColumnManager.OnAddViewLine.AddListener(() =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.AddViewLine();
                for (int i = 0; i < m_ResizableGrid.Columns.Count; i++)
                {
                    m_ResizableGrid.Columns[i].Views.Last().GetComponent<View3DUI>().Initialize(m_Scene, m_Scene.ColumnManager.Columns[i], m_Scene.ColumnManager.Columns[i].Views.Last());
                }
            });
            m_Scene.ColumnManager.OnRemoveViewLine.AddListener((lineID) =>
            {
                if (!m_Scene) return;

                m_ResizableGrid.RemoveViewLine(lineID);
            });
        }
        #endregion
    }
}