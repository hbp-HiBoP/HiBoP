using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Preferences;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using Cysharp.Threading.Tasks;
using System.Linq;

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

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
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

            m_LoadMarsAtlas.onClick.AddListener(async () =>
            {
                if (Object3DManager.MarsAtlas.Loaded)
                {
                    Object3DManager.UnloadMarsAtlas();
                }
                else
                {
                    Object3DManager.MarsAtlas.Load();
                }
                await UniTask.WaitUntil(() => Object3DManager.MarsAtlas.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadJuBrain.onClick.AddListener(async () =>
            {
                if (Object3DManager.JuBrain.Loaded)
                {
                    Object3DManager.UnloadJuBrain();
                }
                else
                {
                    Object3DManager.JuBrain.Load();
                }
                await UniTask.WaitUntil(() => Object3DManager.JuBrain.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadIBC.onClick.AddListener(async () =>
            {
                if (Object3DManager.IBC.Loaded)
                {
                    Object3DManager.UnloadIBC();
                }
                else
                {
                    Object3DManager.IBC.Load();
                }
                await UniTask.WaitUntil(() => Object3DManager.IBC.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo64.onClick.AddListener(async () =>
            {
                if (Object3DManager.DiFuMo.IsLoaded("64"))
                {
                    Object3DManager.UnloadDiFuMo("64");
                }
                else
                {
                    Object3DManager.DiFuMo.Load("64");
                }
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("64"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo128.onClick.AddListener(async () =>
            {
                if (Object3DManager.DiFuMo.IsLoaded("128"))
                {
                    Object3DManager.UnloadDiFuMo("128");
                }
                else
                {
                    Object3DManager.DiFuMo.Load("128");
                }
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("128"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo256.onClick.AddListener(async () =>
            {
                if (Object3DManager.DiFuMo.IsLoaded("256"))
                {
                    Object3DManager.UnloadDiFuMo("256");
                }
                else
                {
                    Object3DManager.DiFuMo.Load("256");
                }
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("256"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo512.onClick.AddListener(async () =>
            {
                if (Object3DManager.DiFuMo.IsLoaded("512"))
                {
                    Object3DManager.UnloadDiFuMo("512");
                }
                else
                {
                    Object3DManager.DiFuMo.Load("512");
                }
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("512"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo1024.onClick.AddListener(async () =>
            {
                if (Object3DManager.DiFuMo.IsLoaded("1024"))
                {
                    Object3DManager.UnloadDiFuMo("1024");
                }
                else
                {
                    Object3DManager.DiFuMo.Load("1024");
                }
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("1024"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadAUDI.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("AUDI");
            });
            m_LoadLEC1.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("LEC1");
            });
            m_LoadLEC2.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("LEC2");
            });
            m_LoadMCSE.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("MCSE");
            });
            m_LoadMOTO.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("MOTO");
            });
            m_LoadMVEB.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("MVEB");
            });
            m_LoadMVIS.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("MVIS");
            });
            m_LoadVISU.onClick.AddListener(async () =>
            {
                await ToggleLocalizerAsync("VISU");
            });

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
            UpdateButtonStatus(Object3DManager.MarsAtlas.Loaded, Object3DManager.MarsAtlas.Loading, m_LoadMarsAtlas, m_LoadMarsAtlasThemeElement);
            UpdateButtonStatus(Object3DManager.JuBrain.Loaded, Object3DManager.JuBrain.Loading, m_LoadJuBrain, m_LoadJuBrainThemeElement);
            UpdateButtonStatus(Object3DManager.IBC.Loaded, Object3DManager.IBC.Loading, m_LoadIBC, m_LoadIBCThemeElement);
            
            UpdateButtonStatus(Object3DManager.DiFuMo.IsLoaded("64"), Object3DManager.DiFuMo.IsLoading("64"), m_LoadDiFuMo64, m_LoadDiFuMo64ThemeElement);
            UpdateButtonStatus(Object3DManager.DiFuMo.IsLoaded("128"), Object3DManager.DiFuMo.IsLoading("128"), m_LoadDiFuMo128, m_LoadDiFuMo128ThemeElement);
            UpdateButtonStatus(Object3DManager.DiFuMo.IsLoaded("256"), Object3DManager.DiFuMo.IsLoading("256"), m_LoadDiFuMo256, m_LoadDiFuMo256ThemeElement);
            UpdateButtonStatus(Object3DManager.DiFuMo.IsLoaded("512"), Object3DManager.DiFuMo.IsLoading("512"), m_LoadDiFuMo512, m_LoadDiFuMo512ThemeElement);
            UpdateButtonStatus(Object3DManager.DiFuMo.IsLoaded("1024"), Object3DManager.DiFuMo.IsLoading("1024"), m_LoadDiFuMo1024, m_LoadDiFuMo1024ThemeElement);

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
        private async UniTask ToggleLocalizerAsync(string protocolName)
        {
            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol != null)
            {
                Object3DManager.UnloadLocalizer(protocolName);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
                return;
            }

            if (!Object3DManager.Localizers.TryLoad(protocolName))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Can not load localizer", $"The localizer {protocolName} could not be loaded. Please make sure you downloaded it and put it in the right folder.").Forget();
                return;
            }

            await UniTask.WaitUntil(() => Object3DManager.Localizers.Protocols.Any(p => p.Name == protocolName && p.Loaded));
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }
        private void UpdateButtonStatus(bool loaded, bool loading, Button button, Theme.ThemeElement element)
        {
            if (loaded)
            {
                button.interactable = true;
                button.GetComponentInChildren<Text>().text = "Unload";
                element.Set(m_LoadedState);
            }
            else if (loading)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loading...";
                element.Set(m_LoadingState);
            }
            else
            {
                button.interactable = true;
                button.GetComponentInChildren<Text>().text = "Load";
                element.Set(m_NotLoadedState);
            }
        }
        private void UpdateLocalizerButtonStatus(string protocolName, Button button, Theme.ThemeElement element)
        {
            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol != null && protocol.Loaded)
            {
                button.interactable = true;
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
                button.interactable = true;
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
        #endregion
    }
}

