using System.Collections.Generic;
using HBP.Data.Experience.Protocol;
using Tools.Unity.Components;
using UnityEngine;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class IconListGestion : ListGestion<Icon>
    {
        #region Properties
        [SerializeField] new IconList List;
        public override List<Icon> Objects
        {
            get
            {
                return base.Objects;
            }

            set
            {
                List.Initialize();
                base.Objects = value;
                List.SortByName(IconList.Sorting.Descending);
            }
        }

        Tools.CSharp.Window m_Window;
        public Tools.CSharp.Window Window
        {
            get
            {
                return m_Window;
            }
            set
            {
                m_Window = value;
                foreach(var modifier in SubWindows.OfType<IconModifier>())
                {
                    modifier.Window = value;
                }
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.List = List;
            base.Initialize();
        }
        protected override void OpenModifier(Icon item, bool interactable)
        {
            IconModifier modifier = ApplicationState.WindowsManager.OpenModifier(item, interactable) as IconModifier;
            modifier.Window = Window;
            modifier.OnClose.AddListener(() => OnCloseSubWindow(modifier));
            modifier.OnSave.AddListener(() => OnSaveModifier(modifier));
            OnOpenSavableWindow.Invoke(modifier);
            SubWindows.Add(modifier);
        }
        #endregion
    }
}