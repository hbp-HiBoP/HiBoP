using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class GraphSettingsWindow : DialogWindow
    {
        #region Properties

        [SerializeField] private ChannelStructGroupsPanel m_ChannelStructGroupsPanel;
        [SerializeField] private LocalizersPanel m_LocalizersPanel;
        [SerializeField] private ColorsPanel m_ColorsPanel;

        public List<ChannelStructsGroup> ChannelStructsGroups
        {
            get => m_ChannelStructGroupsPanel.ChannelStructsGroups;
            set => m_ChannelStructGroupsPanel.ChannelStructsGroups = value;
        }

        #endregion

        #region Events

        public GenericEvent<List<ChannelStructsGroup>> OnDisplayChannelStructsGroupsGraphs => m_ChannelStructGroupsPanel.OnDisplayChannelStructsGroupsGraphs;
        public GenericEvent<Dictionary<ChannelStruct, List<LocalizerCurveData>>> OnGenerateLocalizersGraphs => m_LocalizersPanel.OnGenerateLocalizersGraphs;

        #endregion

        #region Public Methods

        public override void OK()
        {
            base.OK();
            m_ChannelStructGroupsPanel.DisplayGraphs();
        }

        #endregion

        #region Private Methods

        protected override void SetFields()
        {
            base.SetFields();

            m_ChannelStructGroupsPanel.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_LocalizersPanel.Initialize();
        }

        #endregion
    }
}
