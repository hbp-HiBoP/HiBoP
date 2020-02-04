using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CCEPModeSelector : Tool
    {
        #region Properties
        [SerializeField] private Toggle m_Site;
        [SerializeField] private Toggle m_MarsAtlas;
        public GenericEvent<Column3DCCEP.CCEPMode> OnChangeValue = new GenericEvent<Column3DCCEP.CCEPMode>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Site.onValueChanged.AddListener(isOn =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).Mode = Column3DCCEP.CCEPMode.Site;
                OnChangeValue.Invoke(Column3DCCEP.CCEPMode.Site);
            });
            m_MarsAtlas.onValueChanged.AddListener(isOn =>
            {
                if (ListenerLock) return;

                ((Column3DCCEP)SelectedColumn).Mode = Column3DCCEP.CCEPMode.MarsAtlas;
                OnChangeValue.Invoke(Column3DCCEP.CCEPMode.MarsAtlas);
            });
        }
        public override void DefaultState()
        {
            m_Site.isOn = true;
            m_Site.interactable = false;
            m_MarsAtlas.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isColumnCCEP = SelectedColumn is Column3DCCEP;
            m_Site.interactable = isColumnCCEP;
            m_MarsAtlas.interactable = isColumnCCEP && ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.Name == "MarsAtlas") != null;
        }
        #endregion
    }
}