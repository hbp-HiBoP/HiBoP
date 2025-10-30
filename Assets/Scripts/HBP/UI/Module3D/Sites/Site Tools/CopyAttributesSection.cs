using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using System.Collections.Generic;
using HBP.UI.Tools;

namespace HBP.UI.Module3D
{
    public class CopyAttributesSection : SiteToolSection
    {
        #region Properties
        [SerializeField] private Dropdown m_ColumnDropdown;

        public override Base3DScene Scene
        {
            get => m_Scene;
            set
            {
                m_Scene = value;
                m_Columns = value.Columns;
                m_ColumnDropdown.options = m_Columns.Select(c => new Dropdown.OptionData(c.Name)).ToList();
                m_ColumnDropdown.SetValue(m_Columns.IndexOf(value.SelectedColumn));
            }
        }
        private List<Column3D> m_Columns;
        #endregion

        #region Public Methods
        public override async UniTask ApplyAsync()
        {
            var column = m_Columns[m_ColumnDropdown.value];
            var result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Override Sites Attributes", $"The attributes of all sites will be overridden by the attributes of the chosen column ({column.Name}). Do you want to continue?\n\nReminder: a site's attributes consist of its highlighted status, blacklisted status, color and labels.", "Override", "Cancel");
            if (result == 0)
                Scene.ApplySiteStatesToOtherColumns(column);
        }
        public override void StoreSettings()
        {
            // No settings to store
        }
        public override void LoadSettings()
        {
            // No settings to load
        }
        #endregion
    }
}