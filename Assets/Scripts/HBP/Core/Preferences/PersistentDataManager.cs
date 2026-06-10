using HBP.Core.Data;
using HBP.Core.Tools;

namespace HBP.Core.Preferences
{
    public class PersistentDataManager : Manager<PersistentDataManager>
    {
        #region Properties
        private UserPreferences m_UserPreferences;
        public static UserPreferences UserPreferences { get { return m_Instance.m_UserPreferences; } }

        private TagCollection m_Tags;
        public static TagCollection Tags { get { return m_Instance.m_Tags; } }

        private AliasCollection m_Aliases;
        public static AliasCollection Aliases { get { return m_Instance.m_Aliases; } }

        private FilterConditionsPresetCollection m_FilterConditionsPresets;
        public static FilterConditionsPresetCollection FilterConditionsPresets { get { return m_Instance.m_FilterConditionsPresets; } }
        #endregion

        #region Private Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_UserPreferences = UserPreferences.Initialize();
            m_Tags = TagCollection.Initialize();
            m_Aliases = AliasCollection.Initialize();
            m_FilterConditionsPresets = FilterConditionsPresetCollection.Initialize();
        }
        #endregion
    }
}