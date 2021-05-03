using HBP.Data.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
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
            
            m_LoadMarsAtlas.onClick.AddListener(ApplicationState.Module3D.MarsAtlas.Load);
            m_LoadJuBrain.onClick.AddListener(ApplicationState.Module3D.JuBrainAtlas.Load);
            m_LoadIBC.onClick.AddListener(ApplicationState.Module3D.IBCObjects.Load);
            m_LoadDiFuMo64.onClick.AddListener(() => ApplicationState.Module3D.DiFuMoObjects.Load("64"));
            m_LoadDiFuMo128.onClick.AddListener(() => ApplicationState.Module3D.DiFuMoObjects.Load("128"));
            m_LoadDiFuMo256.onClick.AddListener(() => ApplicationState.Module3D.DiFuMoObjects.Load("256"));
            m_LoadDiFuMo512.onClick.AddListener(() => ApplicationState.Module3D.DiFuMoObjects.Load("512"));
            m_LoadDiFuMo1024.onClick.AddListener(() => ApplicationState.Module3D.DiFuMoObjects.Load("1024"));
        }
        #endregion

        #region Protected Methods
        protected void Update()
        {
            if (ApplicationState.Module3D.MarsAtlas.Loaded)
            {
                m_LoadMarsAtlas.interactable = false;
                m_LoadMarsAtlas.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.MarsAtlas.Loading)
            {
                m_LoadMarsAtlas.interactable = false;
                m_LoadMarsAtlas.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.JuBrainAtlas.Loaded)
            {
                m_LoadJuBrain.interactable = false;
                m_LoadJuBrain.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.JuBrainAtlas.Loading)
            {
                m_LoadJuBrain.interactable = false;
                m_LoadJuBrain.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.IBCObjects.Loaded)
            {
                m_LoadIBC.interactable = false;
                m_LoadIBC.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.IBCObjects.Loading)
            {
                m_LoadIBC.interactable = false;
                m_LoadIBC.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.DiFuMoObjects.IsLoaded("64"))
            {
                m_LoadDiFuMo64.interactable = false;
                m_LoadDiFuMo64.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.DiFuMoObjects.IsLoading("64"))
            {
                m_LoadDiFuMo64.interactable = false;
                m_LoadDiFuMo64.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.DiFuMoObjects.IsLoaded("128"))
            {
                m_LoadDiFuMo128.interactable = false;
                m_LoadDiFuMo128.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.DiFuMoObjects.IsLoading("128"))
            {
                m_LoadDiFuMo128.interactable = false;
                m_LoadDiFuMo128.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.DiFuMoObjects.IsLoaded("256"))
            {
                m_LoadDiFuMo256.interactable = false;
                m_LoadDiFuMo256.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.DiFuMoObjects.IsLoading("256"))
            {
                m_LoadDiFuMo256.interactable = false;
                m_LoadDiFuMo256.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.DiFuMoObjects.IsLoaded("512"))
            {
                m_LoadDiFuMo512.interactable = false;
                m_LoadDiFuMo512.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.DiFuMoObjects.IsLoading("512"))
            {
                m_LoadDiFuMo512.interactable = false;
                m_LoadDiFuMo512.GetComponentInChildren<Text>().text = "Loading...";
            }
            if (ApplicationState.Module3D.DiFuMoObjects.IsLoaded("1024"))
            {
                m_LoadDiFuMo1024.interactable = false;
                m_LoadDiFuMo1024.GetComponentInChildren<Text>().text = "Loaded";
            }
            else if (ApplicationState.Module3D.DiFuMoObjects.IsLoading("1024"))
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

