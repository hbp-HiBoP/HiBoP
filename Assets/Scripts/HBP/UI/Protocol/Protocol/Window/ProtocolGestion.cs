﻿using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;
using Tools.Unity;
using HBP.Core.Data;
using HBP.Module3D;

namespace HBP.UI.Experience.Protocol
{
    public class ProtocolGestion : GestionWindow<Core.Data.Protocol>
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ListGestion;
        public override ListGestion<Core.Data.Protocol> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (Core.Data.DataManager.HasData)
            {
                DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.ProjectLoaded.SetProtocols(m_ListGestion.List.Objects);
                    FindObjectOfType<MenuButtonState>().SetInteractables();
                    GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                    LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckDatasets(m_ListGestion.ModifiedProtocols, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
                    Core.Data.DataManager.Clear();
                    HBP3DModule.ReloadScenes();
                });
            }
            else
            {
                base.OK();
                ApplicationState.ProjectLoaded.SetProtocols((m_ListGestion.List.Objects));
                FindObjectOfType<MenuButtonState>().SetInteractables();
                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckDatasets(m_ListGestion.ModifiedProtocols, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            ListGestion.List.Set(ApplicationState.ProjectLoaded.Protocols);
        }
        #endregion
    }
}