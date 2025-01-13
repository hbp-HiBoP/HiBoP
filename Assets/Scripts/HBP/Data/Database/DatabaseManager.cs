using HBP.Core.Tools;

namespace HBP.Data.Database
{
    public class DatabaseManager : Manager<DatabaseManager>
    {
        #region Properties
        private Database m_Database;
        public static Database Database => m_Instance.m_Database;
        #endregion

        #region Public Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_Database = Database.Initialize();
        }
        #endregion
    }
}