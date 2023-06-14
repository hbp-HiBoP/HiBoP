using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Preferences;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.UI.Tools;

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
        [SerializeField] Button m_LoadMarsAtlas;
        [SerializeField] Button m_LoadJuBrain;
        [SerializeField] Button m_LoadIBC;
        [SerializeField] Button m_LoadDiFuMo64;
        [SerializeField] Button m_LoadDiFuMo128;
        [SerializeField] Button m_LoadDiFuMo256;
        [SerializeField] Button m_LoadDiFuMo512;
        [SerializeField] Button m_LoadDiFuMo1024;
        [SerializeField] Button m_MarsAtlasWebsite;
        [SerializeField] Button m_JuBrainWebsite;
        [SerializeField] Button m_IBCWebsite;
        [SerializeField] Button m_DiFuMoWebsite;

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
                m_LoadMarsAtlas.interactable = value;
                m_LoadJuBrain.interactable = value;
                m_LoadIBC.interactable = value;
                m_LoadDiFuMo64.interactable = value;
                m_LoadDiFuMo128.interactable = value;
                m_LoadDiFuMo256.interactable = value;
                m_LoadDiFuMo512.interactable = value;
                m_LoadDiFuMo1024.interactable = value;
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
            
            m_LoadMarsAtlas.onClick.AddListener(() =>
            {
                Object3DManager.MarsAtlas.Load();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadJuBrain.onClick.AddListener(() =>
            {
                Object3DManager.JuBrain.Load();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadIBC.onClick.AddListener(() =>
            {
                Object3DManager.IBC.Load();
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo64.onClick.AddListener(() =>
            {
                Object3DManager.DiFuMo.Load("64");
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo128.onClick.AddListener(() =>
            {
                Object3DManager.DiFuMo.Load("128");
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo256.onClick.AddListener(() =>
            {
                Object3DManager.DiFuMo.Load("256");
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo512.onClick.AddListener(() =>
            {
                Object3DManager.DiFuMo.Load("512");
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });
            m_LoadDiFuMo1024.onClick.AddListener(() =>
            {
                Object3DManager.DiFuMo.Load("1024");
                Module3DMain.OnRequestUpdateInToolbar.Invoke();
            });

            m_MarsAtlasWebsite.onClick.AddListener(() => Application.OpenURL(@"https://meca-brain.org/software/marsatlas/"));
            m_JuBrainWebsite.onClick.AddListener(() => Application.OpenURL(@"https://julich-brain-atlas.de/"));
            m_IBCWebsite.onClick.AddListener(() => Application.OpenURL(@"https://project.inria.fr/IBC/"));
            m_DiFuMoWebsite.onClick.AddListener(() => Application.OpenURL(@"https://github.com/Parietal-INRIA/DiFuMo"));
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
        }
        #endregion
    }
}

