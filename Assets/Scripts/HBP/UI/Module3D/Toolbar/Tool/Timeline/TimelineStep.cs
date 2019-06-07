using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineStep : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Minus;
        [SerializeField]
        private Button m_Plus;
        [SerializeField]
        private InputField m_InputField;

        private bool m_IsGlobal = false;
        /// <summary>
        /// Do we need to perform the actions on all columns ?
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                return m_IsGlobal;
            }
            set
            {
                m_IsGlobal = value;
                if (m_IsGlobal)
                {
                    m_InputField.onEndEdit.Invoke(m_InputField.text);
                }
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Minus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                List<HBP.Module3D.Column3DIEEG> columns = new List<HBP.Module3D.Column3DIEEG>();
                if (IsGlobal)
                {
                    columns = SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)SelectedColumn);
                }
                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.Timeline.CurrentIndex -= column.Timeline.Step;
                }
            });

            m_Plus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                List<HBP.Module3D.Column3DIEEG> columns = new List<HBP.Module3D.Column3DIEEG>();
                if (IsGlobal)
                {
                    columns = SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)SelectedColumn);
                }
                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.Timeline.CurrentIndex += column.Timeline.Step;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                int val = 1;
                int step = 1;
                if (int.TryParse(value, out val))
                {
                    step = val;
                    if (step < 1)
                    {
                        step = 1;
                        val = 1;
                    }
                    m_InputField.text = val.ToString();
                }
                else
                {
                    step = 1;
                    val = 1;
                    m_InputField.text = val.ToString();
                }

                List<HBP.Module3D.Column3DIEEG> columns = new List<HBP.Module3D.Column3DIEEG>();
                if (IsGlobal)
                {
                    columns = SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)SelectedColumn);
                }

                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.Timeline.Step = step;
                }
            });
        }

        public override void DefaultState()
        {
            m_Minus.interactable = false;
            m_Plus.interactable = false;
            m_InputField.text = "1";
            m_InputField.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn.Type == Data.Enums.ColumnType.iEEG;
            bool areAmplitudesComputed = SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Minus.interactable = isColumnIEEG && areAmplitudesComputed;
            m_InputField.interactable = isColumnIEEG && areAmplitudesComputed;
            m_Plus.interactable = isColumnIEEG && areAmplitudesComputed;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn.Type == Data.Enums.ColumnType.iEEG)
            {
                m_InputField.text = ((HBP.Module3D.Column3DIEEG)SelectedColumn).Timeline.Step.ToString();
            }
            else
            {
                m_InputField.text = "1";
            }
        }
        #endregion
    }
}