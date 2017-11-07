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
                    columns = ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
                }
                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.CurrentTimeLineID -= column.TimelineStep;
                }
            });

            m_Plus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                List<HBP.Module3D.Column3DIEEG> columns = new List<HBP.Module3D.Column3DIEEG>();
                if (IsGlobal)
                {
                    columns = ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
                }
                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.CurrentTimeLineID += column.TimelineStep;
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
                    columns = ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG.ToList();
                }
                else
                {
                    columns.Add((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn);
                }

                foreach (HBP.Module3D.Column3DIEEG column in columns)
                {
                    column.TimelineStep = step;
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
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Minus.interactable = isColumnIEEG;
                    m_InputField.interactable = isColumnIEEG;
                    m_Plus.interactable = isColumnIEEG;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Minus.interactable = false;
                    m_InputField.interactable = false;
                    m_Plus.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                if (ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG)
                {
                    m_InputField.text = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).TimelineStep.ToString();
                }
                else
                {
                    m_InputField.text = "1";
                }
            }
        }
        #endregion
    }
}