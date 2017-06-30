using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ROICopy : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnChangeNumberOfROI.AddListener(() =>
            {
                UpdateInteractable();
            });
            m_Button.onClick.AddListener(() =>
            {
                foreach (Column3D column in ApplicationState.Module3D.SelectedScene.ColumnManager.Columns)
                {
                    if (column == ApplicationState.Module3D.SelectedColumn) continue;

                    foreach (ROI roi in ApplicationState.Module3D.SelectedColumn.ROIs)
                    {
                        column.CopyROI(roi);
                    }
                }

                foreach (Column3D column in ApplicationState.Module3D.SelectedScene.ColumnManager.Columns)
                {
                    ApplicationState.Module3D.SelectedScene.UpdateCurrentRegionOfInterest(column);
                }
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
            m_Button.gameObject.SetActive(false);
        }
        public override void UpdateInteractable()
        {
            bool hasROI = ApplicationState.Module3D.SelectedColumn.ROIs.Count > 0;
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Button.interactable = false;
                    m_Button.gameObject.SetActive(false);
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Button.interactable = hasROI;
                    m_Button.gameObject.SetActive(hasROI);
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Button.interactable = hasROI;
                    m_Button.gameObject.SetActive(hasROI);
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Button.interactable = false;
                    m_Button.gameObject.SetActive(false);
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Button.interactable = hasROI;
                    m_Button.gameObject.SetActive(hasROI);
                    break;
                case Mode.ModesId.TriErasing:
                    m_Button.interactable = false;
                    m_Button.gameObject.SetActive(false);
                    break;
                case Mode.ModesId.ROICreation:
                    m_Button.interactable = hasROI;
                    m_Button.gameObject.SetActive(hasROI);
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Button.interactable = hasROI;
                    m_Button.gameObject.SetActive(hasROI);
                    break;
                case Mode.ModesId.Error:
                    m_Button.interactable = false;
                    m_Button.gameObject.SetActive(false);
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                UpdateInteractable();
            }
        }
        #endregion
    }
}