using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TimelineLoop : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private Toggle m_Toggle;
        [SerializeField]
        private Slider m_Slider;

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
                    // Synchronize columns
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IsLooping = false;
                    }
                    m_Toggle.onValueChanged.Invoke(m_Toggle.isOn);
                    m_Slider.onValueChanged.Invoke(m_Slider.value);
                }
            }
        }

        public GenericEvent<bool, float> OnChangeValue = new GenericEvent<bool, float>();
        #endregion

        #region Public Methods
        public override void AddListeners()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
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
                    column.IsLooping = m_Toggle.isOn;
                }
                OnChangeValue.Invoke(isOn, m_Slider.value);
            });

            m_Slider.onValueChanged.AddListener((value) =>
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
                    column.LoopingSpeed = value;
                }
                OnChangeValue.Invoke(m_Toggle.isOn, value);
            });
        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
            m_Slider.value = 0.25f;
            m_Slider.interactable = false;
        }

        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Button.interactable = true;
                    m_Slider.interactable = true;
                    m_Toggle.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Button.interactable = false;
                    m_Slider.interactable = false;
                    m_Toggle.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                m_Toggle.isOn = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).IsLooping;
                m_Slider.value = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).LoopingSpeed;
            }
        }
        #endregion
    }
}