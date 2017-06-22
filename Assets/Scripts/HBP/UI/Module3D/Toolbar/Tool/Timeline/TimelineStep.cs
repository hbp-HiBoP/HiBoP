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

        private int m_Step = 1;
        
        /// <summary>
        /// Do we need to perform the actions on all columns ?
        /// </summary>
        public bool IsGlobal { get; set; }

        public GenericEvent<int> OnChangeValue = new GenericEvent<int>();
        #endregion

        #region Public Methods
        public override void AddListeners()
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
                    column.CurrentTimeLineID -= m_Step;
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
                    column.CurrentTimeLineID += m_Step;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                int val = 1;
                if (int.TryParse(value, out val))
                {
                    m_Step = val;
                    m_InputField.text = val.ToString();
                }
                else
                {
                    m_Step = 1;
                    val = 1;
                    m_InputField.text = val.ToString();
                }
                OnChangeValue.Invoke(val);
            });
        }

        public override void DefaultState()
        {
            m_Minus.interactable = false;
            m_Plus.interactable = false;
            m_InputField.text = "1";
            m_Step = 1;
            m_InputField.interactable = false;
        }

        public override void UpdateInteractable()
        {
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
                    m_Minus.interactable = true;
                    m_InputField.interactable = true;
                    m_Plus.interactable = true;
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
        }
        #endregion
    }
}