using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Preferences;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using HBP.Quest.Desktop;

namespace HBP.UI.Main
{
    public class AtlasesPreferencesSubModifier : SubModifier<AtlasesPreferences>
    {
        #region Properties

        [SerializeField] Toggle m_MarsAtlas;
        [SerializeField] Toggle m_JuBrain;
        [SerializeField] Toggle m_IBC;
        [SerializeField] Toggle m_DiFuMo64;
        [SerializeField] Toggle m_DiFuMo128;
        [SerializeField] Toggle m_DiFuMo256;
        [SerializeField] Toggle m_DiFuMo512;
        [SerializeField] Toggle m_DiFuMo1024;
        [SerializeField] Toggle m_AUDI;
        [SerializeField] Toggle m_LEC1;
        [SerializeField] Toggle m_LEC2;
        [SerializeField] Toggle m_MCSE;
        [SerializeField] Toggle m_MOTO;
        [SerializeField] Toggle m_MVEB;
        [SerializeField] Toggle m_MVIS;
        [SerializeField] Toggle m_VISU;

        [SerializeField] Button m_LoadMarsAtlas;
        [SerializeField] Button m_LoadJuBrain;
        [SerializeField] Button m_LoadIBC;
        [SerializeField] Button m_LoadDiFuMo64;
        [SerializeField] Button m_LoadDiFuMo128;
        [SerializeField] Button m_LoadDiFuMo256;
        [SerializeField] Button m_LoadDiFuMo512;
        [SerializeField] Button m_LoadDiFuMo1024;
        [SerializeField] Button m_LoadAUDI;
        [SerializeField] Button m_LoadLEC1;
        [SerializeField] Button m_LoadLEC2;
        [SerializeField] Button m_LoadMCSE;
        [SerializeField] Button m_LoadMOTO;
        [SerializeField] Button m_LoadMVEB;
        [SerializeField] Button m_LoadMVIS;
        [SerializeField] Button m_LoadVISU;

        [SerializeField] Theme.ThemeElement m_LoadMarsAtlasThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadJuBrainThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadIBCThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadDiFuMo64ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadDiFuMo128ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadDiFuMo256ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadDiFuMo512ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadDiFuMo1024ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadAUDIThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadLEC1ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadLEC2ThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadMCSEThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadMOTOThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadMVEBThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadMVISThemeElement;
        [SerializeField] Theme.ThemeElement m_LoadVISUThemeElement;

        [SerializeField] Theme.State m_NotLoadedState;
        [SerializeField] Theme.State m_LoadingState;
        [SerializeField] Theme.State m_LoadedState;

        [SerializeField] Button m_MarsAtlasWebsite;
        [SerializeField] Button m_JuBrainWebsite;
        [SerializeField] Button m_IBCWebsite;
        [SerializeField] Button m_DiFuMoWebsite;
        [SerializeField] Button m_LocalizersWebsite;
        private readonly Dictionary<string, bool> m_PendingAtlasOperations = new(StringComparer.Ordinal);

        public override bool Interactable
        {
            get { return base.Interactable; }
            set
            {
                base.Interactable = value;

                m_MarsAtlas.interactable = value;
                m_JuBrain.interactable = value;
                m_IBC.interactable = value;
                m_DiFuMo64.interactable = value;
                m_DiFuMo128.interactable = value;
                m_DiFuMo256.interactable = value;
                m_DiFuMo512.interactable = value;
                m_DiFuMo1024.interactable = value;
                m_AUDI.interactable = Object3DManager.Localizers.IsAvailable("AUDI");
                m_LEC1.interactable = Object3DManager.Localizers.IsAvailable("LEC1");
                m_LEC2.interactable = Object3DManager.Localizers.IsAvailable("LEC2");
                m_MCSE.interactable = Object3DManager.Localizers.IsAvailable("MCSE");
                m_MOTO.interactable = Object3DManager.Localizers.IsAvailable("MOTO");
                m_MVEB.interactable = Object3DManager.Localizers.IsAvailable("MVEB");
                m_MVIS.interactable = Object3DManager.Localizers.IsAvailable("MVIS");
                m_VISU.interactable = Object3DManager.Localizers.IsAvailable("VISU");

                m_LoadMarsAtlas.interactable = value;
                m_LoadJuBrain.interactable = value;
                m_LoadIBC.interactable = value;
                m_LoadDiFuMo64.interactable = value;
                m_LoadDiFuMo128.interactable = value;
                m_LoadDiFuMo256.interactable = value;
                m_LoadDiFuMo512.interactable = value;
                m_LoadDiFuMo1024.interactable = value;
                m_LoadAUDI.interactable = value;
                m_LoadLEC1.interactable = value;
                m_LoadLEC2.interactable = value;
                m_LoadMCSE.interactable = value;
                m_LoadMOTO.interactable = value;
                m_LoadMVEB.interactable = value;
                m_LoadMVIS.interactable = value;
                m_LoadVISU.interactable = value;
            }
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_MarsAtlas.onValueChanged.AddListener(value => Object.PreloadMarsAtlas = value);
            m_JuBrain.onValueChanged.AddListener(value => Object.PreloadJuBrain = value);
            m_IBC.onValueChanged.AddListener(value => Object.PreloadIBC = value);
            m_DiFuMo64.onValueChanged.AddListener(value => Object.PreloadDiFuMo64 = value);
            m_DiFuMo128.onValueChanged.AddListener(value => Object.PreloadDiFuMo128 = value);
            m_DiFuMo256.onValueChanged.AddListener(value => Object.PreloadDiFuMo256 = value);
            m_DiFuMo512.onValueChanged.AddListener(value => Object.PreloadDiFuMo512 = value);
            m_DiFuMo1024.onValueChanged.AddListener(value => Object.PreloadDiFuMo1024 = value);
            m_AUDI.onValueChanged.AddListener(value => Object.PreloadLocalizerAUDI = value);
            m_LEC1.onValueChanged.AddListener(value => Object.PreloadLocalizerLEC1 = value);
            m_LEC2.onValueChanged.AddListener(value => Object.PreloadLocalizerLEC2 = value);
            m_MCSE.onValueChanged.AddListener(value => Object.PreloadLocalizerMCSE = value);
            m_MOTO.onValueChanged.AddListener(value => Object.PreloadLocalizerMOTO = value);
            m_MVEB.onValueChanged.AddListener(value => Object.PreloadLocalizerMVEB = value);
            m_MVIS.onValueChanged.AddListener(value => Object.PreloadLocalizerMVIS = value);
            m_VISU.onValueChanged.AddListener(value => Object.PreloadLocalizerVISU = value);

            m_LoadMarsAtlas.onClick.AddListener(async () => await ToggleAtlasAsync("mars"));
            m_LoadJuBrain.onClick.AddListener(async () => await ToggleAtlasAsync("jubrain"));
            m_LoadIBC.onClick.AddListener(async () => await ToggleAtlasAsync("ibc"));
            m_LoadDiFuMo64.onClick.AddListener(async () => await ToggleAtlasAsync("difumo:64"));
            m_LoadDiFuMo128.onClick.AddListener(async () => await ToggleAtlasAsync("difumo:128"));
            m_LoadDiFuMo256.onClick.AddListener(async () => await ToggleAtlasAsync("difumo:256"));
            m_LoadDiFuMo512.onClick.AddListener(async () => await ToggleAtlasAsync("difumo:512"));
            m_LoadDiFuMo1024.onClick.AddListener(async () => await ToggleAtlasAsync("difumo:1024"));
            m_LoadAUDI.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:AUDI"));
            m_LoadLEC1.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:LEC1"));
            m_LoadLEC2.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:LEC2"));
            m_LoadMCSE.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:MCSE"));
            m_LoadMOTO.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:MOTO"));
            m_LoadMVEB.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:MVEB"));
            m_LoadMVIS.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:MVIS"));
            m_LoadVISU.onClick.AddListener(async () => await ToggleAtlasAsync("localizer:VISU"));
            m_MarsAtlasWebsite.onClick.AddListener(() => Application.OpenURL(@"https://meca-brain.org/software/marsatlas/"));
            m_JuBrainWebsite.onClick.AddListener(() => Application.OpenURL(@"https://julich-brain-atlas.de/"));
            m_IBCWebsite.onClick.AddListener(() => Application.OpenURL(@"https://individual-brain-charting.github.io/docs/"));
            m_DiFuMoWebsite.onClick.AddListener(() => Application.OpenURL(@"https://parietal-inria.github.io/DiFuMo/"));
            m_LocalizersWebsite.onClick.AddListener(() => Application.OpenURL(@"https://github.com/CRNL-Eduwell/Localizer"));
        }

        #endregion

        #region Protected Methods

        protected void Update()
        {
            UpdateButtonStatus("mars", m_LoadMarsAtlas, m_LoadMarsAtlasThemeElement);
            UpdateButtonStatus("jubrain", m_LoadJuBrain, m_LoadJuBrainThemeElement);
            UpdateButtonStatus("ibc", m_LoadIBC, m_LoadIBCThemeElement);

            UpdateButtonStatus("difumo:64", m_LoadDiFuMo64, m_LoadDiFuMo64ThemeElement);
            UpdateButtonStatus("difumo:128", m_LoadDiFuMo128, m_LoadDiFuMo128ThemeElement);
            UpdateButtonStatus("difumo:256", m_LoadDiFuMo256, m_LoadDiFuMo256ThemeElement);
            UpdateButtonStatus("difumo:512", m_LoadDiFuMo512, m_LoadDiFuMo512ThemeElement);
            UpdateButtonStatus("difumo:1024", m_LoadDiFuMo1024, m_LoadDiFuMo1024ThemeElement);

            UpdateLocalizerButtonStatus("AUDI", m_LoadAUDI, m_LoadAUDIThemeElement);
            UpdateLocalizerButtonStatus("LEC1", m_LoadLEC1, m_LoadLEC1ThemeElement);
            UpdateLocalizerButtonStatus("LEC2", m_LoadLEC2, m_LoadLEC2ThemeElement);
            UpdateLocalizerButtonStatus("MCSE", m_LoadMCSE, m_LoadMCSEThemeElement);
            UpdateLocalizerButtonStatus("MOTO", m_LoadMOTO, m_LoadMOTOThemeElement);
            UpdateLocalizerButtonStatus("MVEB", m_LoadMVEB, m_LoadMVEBThemeElement);
            UpdateLocalizerButtonStatus("MVIS", m_LoadMVIS, m_LoadMVISThemeElement);
            UpdateLocalizerButtonStatus("VISU", m_LoadVISU, m_LoadVISUThemeElement);
        }

        protected override void SetFields(AtlasesPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_MarsAtlas.isOn = objectToDisplay.PreloadMarsAtlas;
            m_JuBrain.isOn = objectToDisplay.PreloadJuBrain;
            m_IBC.isOn = objectToDisplay.PreloadIBC;
            m_DiFuMo64.isOn = objectToDisplay.PreloadDiFuMo64;
            m_DiFuMo128.isOn = objectToDisplay.PreloadDiFuMo128;
            m_DiFuMo256.isOn = objectToDisplay.PreloadDiFuMo256;
            m_DiFuMo512.isOn = objectToDisplay.PreloadDiFuMo512;
            m_DiFuMo1024.isOn = objectToDisplay.PreloadDiFuMo1024;
            m_AUDI.isOn = objectToDisplay.PreloadLocalizerAUDI;
            m_LEC1.isOn = objectToDisplay.PreloadLocalizerLEC1;
            m_LEC2.isOn = objectToDisplay.PreloadLocalizerLEC2;
            m_MCSE.isOn = objectToDisplay.PreloadLocalizerMCSE;
            m_MOTO.isOn = objectToDisplay.PreloadLocalizerMOTO;
            m_MVEB.isOn = objectToDisplay.PreloadLocalizerMVEB;
            m_MVIS.isOn = objectToDisplay.PreloadLocalizerMVIS;
            m_VISU.isOn = objectToDisplay.PreloadLocalizerVISU;
        }

        private async UniTask ToggleAtlasAsync(string id)
        {
            if (m_PendingAtlasOperations.ContainsKey(id)) return;
            bool load = !AtlasResources.IsLoaded(id);
            // Disable duplicate clicks until the local native operation finishes.
            m_PendingAtlasOperations.Add(id, load);
            try
            {
                if (QuestManager.IsInitialized) await QuestManager.Instance.SetAtlasLoadedAsync(id, load);
                else if (load)
                {
                    var result = await AtlasResources.LoadAsync(id);
                    if (!result.Succeeded) throw new InvalidOperationException(result.Error);
                }
                else AtlasResources.Unload(id);

                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            }
            catch (Exception exception)
            {
                await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Error, "Atlas operation failed", exception.Message, "OK");
            }
            finally
            {
                m_PendingAtlasOperations.Remove(id);
            }
        }

        private bool UpdatePendingButtonStatus(string id, Button button, Theme.ThemeElement element)
        {
            if (!m_PendingAtlasOperations.TryGetValue(id, out bool load)) return false;
            button.interactable = false;
            button.GetComponentInChildren<Text>().text = load ? "Loading..." : "Unloading...";
            element.Set(m_LoadingState);
            return true;
        }

        private void UpdateButtonStatus(string id, Button button, Theme.ThemeElement element)
        {
            UpdateAtlasTooltip(id, button);
            if (UpdatePendingButtonStatus(id, button, element)) return;
            if (AtlasResources.IsLoaded(id))
            {
                button.interactable = m_Interactable;
                button.GetComponentInChildren<Text>().text = "Unload";
                element.Set(m_LoadedState);
            }
            else if (AtlasResources.Status(id).State == AtlasLoadState.Loading)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loading...";
                element.Set(m_LoadingState);
            }
            else
            {
                button.interactable = m_Interactable;
                button.GetComponentInChildren<Text>().text = "Load";
                element.Set(m_NotLoadedState);
            }
        }

        private void UpdateLocalizerButtonStatus(string protocolName, Button button, Theme.ThemeElement element)
        {
            UpdateAtlasTooltip("localizer:" + protocolName, button);
            if (UpdatePendingButtonStatus("localizer:" + protocolName, button, element)) return;
            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (AtlasResources.Status("localizer:" + protocolName).State == AtlasLoadState.Loading)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loading...";
                element.Set(m_LoadingState);
            }
            else if (protocol != null && protocol.Loaded)
            {
                button.interactable = m_Interactable;
                button.GetComponentInChildren<Text>().text = "Unload";
                element.Set(m_LoadedState);
            }
            else if (protocol != null)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loading...";
                element.Set(m_LoadingState);
            }
            else if (Object3DManager.Localizers.IsAvailable(protocolName))
            {
                button.interactable = m_Interactable;
                button.GetComponentInChildren<Text>().text = "Load";
                element.Set(m_NotLoadedState);
            }
            else
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Not available";
                element.Set(m_NotLoadedState);
            }
        }

        private static void UpdateAtlasTooltip(string id, Button button)
        {
            var tooltip = button.GetComponent<Tooltip>();
            if (tooltip != null) tooltip.Text = QuestManager.IsInitialized ? QuestManager.Instance.GetAtlasSynchronizationStatus(id) : AtlasResources.IsLoaded(id) ? "Loaded and usable on Desktop." : "Load this atlas on Desktop.";
        }

        #endregion
    }
}
