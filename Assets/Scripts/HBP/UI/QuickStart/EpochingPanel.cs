using HBP.Data.Experience.Protocol;
using System.Collections;
using System.Collections.Generic;
using Tools.CSharp;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
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
            m_Window.minLimit = ApplicationState.UserPreferences.Data.Protocol.MinLimit;
            m_Window.maxLimit = ApplicationState.UserPreferences.Data.Protocol.MaxLimit;
            m_Window.step = ApplicationState.UserPreferences.Data.Protocol.Step;
            m_Window.Values = new Vector2(-500, 500);
        }
        #endregion

        #region Public Methods
        public override bool OpenNextPanel()
        {
            Tools.CSharp.Window window = new Tools.CSharp.Window((int)m_Window.Values.x, (int)m_Window.Values.y);
            if (window.Lenght == 0)
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Window length is zero", "The length of the window needs to be strictly above zero in order to continue.");
                return false;
            }
            List<Bloc> blocs = new List<Bloc>();
            foreach (var codeString in m_Codes.text.Split(','))
            {
                if (int.TryParse(codeString, out int code))
                {
                    Data.Experience.Protocol.Event ev = new Data.Experience.Protocol.Event(string.Format("QS{0}", code), new int[] { code },Data.Enums.MainSecondaryEnum.Main );
                    SubBloc subBloc = new SubBloc(string.Format("QS{0}", code), 0, Data.Enums.MainSecondaryEnum.Main, window, new Tools.CSharp.Window(0, 0), new Data.Experience.Protocol.Event[] { ev }, new Icon[0], new Treatment[0]);
                    Bloc bloc = new Bloc(string.Format("QS{0}", code), 0, "", "", new SubBloc[] { subBloc });
                    blocs.Add(bloc);
                }
                if (blocs.Count == 0)
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No code found", "You need to put at least one code in the input field in order to continue.");
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