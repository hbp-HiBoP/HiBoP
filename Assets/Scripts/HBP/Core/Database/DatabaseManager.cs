using HBP.Core.Tools;

namespace HBP.Core.Database
{
    public class DatabaseManager : Manager<DatabaseManager>
    {
        #region Properties
        private GlobalDatabase m_Database;
        public static GlobalDatabase Database => m_Instance.m_Database;
        #endregion

        #region Public Methods
        protected override void Initialization()
        {
            base.Initialization();
            m_Database = new GlobalDatabase();
        }
        #endregion
    }
}
