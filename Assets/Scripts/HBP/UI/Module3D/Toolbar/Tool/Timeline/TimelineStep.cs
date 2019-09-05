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

                foreach (HBP.Module3D.Column3DDynamic column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.Timeline.CurrentIndex -= column.Timeline.Step;
                }
            });

            m_Plus.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                foreach (HBP.Module3D.Column3DDynamic column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.Timeline.CurrentIndex += column.Timeline.Step;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                int step = 1;
                if (int.TryParse(value, out int val))
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

                foreach (HBP.Module3D.Column3DDynamic column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
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
            bool isColumnDynamic = SelectedColumn is HBP.Module3D.Column3DDynamic;
            bool areAmplitudesComputed = SelectedScene.SceneInformation.IsGeneratorUpToDate;

            m_Minus.interactable = isColumnDynamic && areAmplitudesComputed;
            m_InputField.interactable = isColumnDynamic && areAmplitudesComputed;
            m_Plus.interactable = isColumnDynamic && areAmplitudesComputed;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn is HBP.Module3D.Column3DDynamic dynamicColumn)
            {
                m_InputField.text = dynamicColumn.Timeline.Step.ToString();
            }
            else
            {
                m_InputField.text = "1";
            }
        }
        #endregion
    }
}