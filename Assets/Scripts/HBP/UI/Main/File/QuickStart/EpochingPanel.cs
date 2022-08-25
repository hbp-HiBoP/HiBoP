using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;

namespace HBP.UI.Main.QuickStart
{
    public class EpochingPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private Toggle m_AlreadyEpoched;
        [SerializeField] private Toggle m_NeedsEpoching;

        [SerializeField] private RectTransform m_EpochingPanel;
        [SerializeField] private InputField m_Codes;
        [SerializeField] private RangeSlider m_Window;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_Window.minLimit = PreferencesManager.UserPreferences.Data.Protocol.MinLimit;
            m_Window.maxLimit = PreferencesManager.UserPreferences.Data.Protocol.MaxLimit;
            m_Window.step = PreferencesManager.UserPreferences.Data.Protocol.Step;
            m_Window.Values = new Vector2(-500, 500);
        }
        #endregion

        #region Public Methods
        public override bool OpenNextPanel()
        {
            Core.Tools.TimeWindow window = new Core.Tools.TimeWindow((int)m_Window.Values.x, (int)m_Window.Values.y);
            if (window.Lenght == 0)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Window length is zero", "The length of the window needs to be strictly above zero in order to continue.");
                return false;
            }
            List<Bloc> blocs = new List<Bloc>();
            foreach (var codeString in m_Codes.text.Split(','))
            {
                if (int.TryParse(codeString, out int code))
                {
                    Core.Data.Event ev = new Core.Data.Event(string.Format("QS{0}", code), new int[] { code }, MainSecondaryEnum.Main );
                    SubBloc subBloc = new SubBloc(string.Format("QS{0}", code), 0, MainSecondaryEnum.Main, window, new Core.Tools.TimeWindow(0, 0), new Core.Data.Event[] { ev }, new Icon[0], new Treatment[0]);
                    Bloc bloc = new Bloc(string.Format("QS{0}", code), 0, "", "", new SubBloc[] { subBloc });
                    blocs.Add(bloc);
                }
                if (blocs.Count == 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No code found", "You need to put at least one code in the input field in order to continue.");
                    return false;
                }
            }
            Protocol protocol = new Protocol("QuickStart", blocs);
            ApplicationState.ProjectLoaded.SetProtocols(new Protocol[] { protocol });
            return base.OpenNextPanel();
        }
        public override bool OpenPreviousPanel()
        {
            ApplicationState.ProjectLoaded.SetProtocols(new Protocol[0]);
            return base.OpenPreviousPanel();
        }
        #endregion
    }
}