using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Preferences;
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
                Object3DManager.MarsAtlas.Load();
                await UniTask.WaitUntil(() => Object3DManager.MarsAtlas.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadJuBrain.onClick.AddListener(async () =>
            {
                Object3DManager.JuBrain.Load();
                await UniTask.WaitUntil(() => Object3DManager.JuBrain.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadIBC.onClick.AddListener(async () =>
            {
                Object3DManager.IBC.Load();
                await UniTask.WaitUntil(() => Object3DManager.IBC.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo64.onClick.AddListener(async () =>
            {
                Object3DManager.DiFuMo.Load("64");
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("64"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo128.onClick.AddListener(async () =>
            {
                Object3DManager.DiFuMo.Load("128");
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("128"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo256.onClick.AddListener(async () =>
            {
                Object3DManager.DiFuMo.Load("256");
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("256"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo512.onClick.AddListener(async () =>
            {
                Object3DManager.DiFuMo.Load("512");
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("512"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo1024.onClick.AddListener(async () =>
            {
                Object3DManager.DiFuMo.Load("1024");
                await UniTask.WaitUntil(() => Object3DManager.DiFuMo.IsLoaded("1024"));
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadAUDI.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("AUDI");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadLEC1.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("LEC1");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadLEC2.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("LEC2");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadMCSE.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("MCSE");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadMOTO.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("MOTO");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadMVEB.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("MVEB");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadMVIS.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("MVIS");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadVISU.onClick.AddListener(async () =>
            {
                Object3DManager.Localizers.Load("VISU");
                await UniTask.WaitUntil(() => Object3DManager.Localizers.Loaded);
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
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
            if (Object3DManager.MarsAtlas.Loaded)
            {
                m_LoadMarsAtlas.interactable = false;
                m_LoadMarsAtlas.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.MarsAtlas.Loading)
            {
                m_LoadMarsAtlas.interactable = false;
                m_LoadMarsAtlas.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.JuBrain.Loaded)
            {
                m_LoadJuBrain.interactable = false;
                m_LoadJuBrain.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.JuBrain.Loading)
            {
                m_LoadJuBrain.interactable = false;
                m_LoadJuBrain.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.IBC.Loaded)
            {
                m_LoadIBC.interactable = false;
                m_LoadIBC.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.IBC.Loading)
            {
                m_LoadIBC.interactable = false;
                m_LoadIBC.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.DiFuMo.IsLoaded("64"))
            {
                m_LoadDiFuMo64.interactable = false;
                m_LoadDiFuMo64.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.DiFuMo.IsLoading("64"))
            {
                m_LoadDiFuMo64.interactable = false;
                m_LoadDiFuMo64.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.DiFuMo.IsLoaded("128"))
            {
                m_LoadDiFuMo128.interactable = false;
                m_LoadDiFuMo128.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.DiFuMo.IsLoading("128"))
            {
                m_LoadDiFuMo128.interactable = false;
                m_LoadDiFuMo128.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.DiFuMo.IsLoaded("256"))
            {
                m_LoadDiFuMo256.interactable = false;
                m_LoadDiFuMo256.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.DiFuMo.IsLoading("256"))
            {
                m_LoadDiFuMo256.interactable = false;
                m_LoadDiFuMo256.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.DiFuMo.IsLoaded("512"))
            {
                m_LoadDiFuMo512.interactable = false;
                m_LoadDiFuMo512.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.DiFuMo.IsLoading("512"))
            {
                m_LoadDiFuMo512.interactable = false;
                m_LoadDiFuMo512.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (Object3DManager.DiFuMo.IsLoaded("1024"))
            {
                m_LoadDiFuMo1024.interactable = false;
                m_LoadDiFuMo1024.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (Object3DManager.DiFuMo.IsLoading("1024"))
            {
                m_LoadDiFuMo1024.interactable = false;
                m_LoadDiFuMo1024.GetComponentInChildren<Text>().text = "Loading...";
            }

            // Update Localizers status
            UpdateLocalizerButtonStatus("AUDI", m_LoadAUDI);
            UpdateLocalizerButtonStatus("LEC1", m_LoadLEC1);
            UpdateLocalizerButtonStatus("LEC2", m_LoadLEC2);
            UpdateLocalizerButtonStatus("MCSE", m_LoadMCSE);
            UpdateLocalizerButtonStatus("MOTO", m_LoadMOTO);
            UpdateLocalizerButtonStatus("MVEB", m_LoadMVEB);
            UpdateLocalizerButtonStatus("MVIS", m_LoadMVIS);
            UpdateLocalizerButtonStatus("VISU", m_LoadVISU);
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
        private void UpdateLocalizerButtonStatus(string protocolName, Button button)
        {
            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol != null && protocol.Loaded)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (protocol != null)
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Loading...";
            }
            else if (Object3DManager.Localizers.IsAvailable(protocolName))
            {
                button.interactable = true;
                button.GetComponentInChildren<Text>().text = "Load";
            }
            else
            {
                button.interactable = false;
                button.GetComponentInChildren<Text>().text = "Not available";
            }
        }
        #endregion
    }
}

