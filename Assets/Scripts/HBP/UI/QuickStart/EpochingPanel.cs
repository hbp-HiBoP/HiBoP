using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Core.Data;

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
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Window length is zero", "The length of the window needs to be strictly above zero in order to continue.");
                return false;
            }
            List<Core.Data.Bloc> blocs = new List<Core.Data.Bloc>();
            foreach (var codeString in m_Codes.text.Split(','))
            {
                if (int.TryParse(codeString, out int code))
                {
                    Core.Data.Event ev = new Core.Data.Event(string.Format("QS{0}", code), new int[] { code }, MainSecondaryEnum.Main );
                    Core.Data.SubBloc subBloc = new Core.Data.SubBloc(string.Format("QS{0}", code), 0, MainSecondaryEnum.Main, window, new Tools.CSharp.Window(0, 0), new Core.Data.Event[] { ev }, new Core.Data.Icon[0], new Core.Data.Treatment[0]);
                    Core.Data.Bloc bloc = new Core.Data.Bloc(string.Format("QS{0}", code), 0, "", "", new Core.Data.SubBloc[] { subBloc });
                    blocs.Add(bloc);
                }
                if (blocs.Count == 0)
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No code found", "You need to put at least one code in the input field in order to continue.");
                    return false;
                }
            }
            Core.Data.Protocol protocol = new Core.Data.Protocol("QuickStart", blocs);
            ApplicationState.ProjectLoaded.SetProtocols(new Core.Data.Protocol[] { protocol });
            return base.OpenNextPanel();
        }
        public override bool OpenPreviousPanel()
        {
            ApplicationState.ProjectLoaded.SetProtocols(new Core.Data.Protocol[0]);
            return base.OpenPreviousPanel();
        }
        #endregion
    }
}