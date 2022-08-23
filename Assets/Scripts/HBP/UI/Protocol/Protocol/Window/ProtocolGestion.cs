using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;
using HBP.Core.Data;
using HBP.Display.Module3D;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class ProtocolGestion : GestionWindow<Protocol>
    {
        #region Properties
        [SerializeField] ProtocolListGestion m_ListGestion;
        public override ListGestion<Protocol> ListGestion => m_ListGestion;
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (DataManager.HasData)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    base.OK();
                    ApplicationState.ProjectLoaded.SetProtocols(m_ListGestion.List.Objects);
                    FindObjectOfType<MenuButtonState>().SetInteractables();
                    GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                    LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckDatasets(m_ListGestion.ModifiedProtocols, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
                    DataManager.Clear();
                    Module3DMain.ReloadScenes();
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