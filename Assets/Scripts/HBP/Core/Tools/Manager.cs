using UnityEngine;

namespace HBP.Core.Tools
{
    public class Manager<T> : Singleton<T> where T : MonoBehaviour
    {
        #region Private Methods

        protected override void Initialization()
        {
            base.Initialization();
            DontDestroyOnLoad(this);
        }

        #endregion
    }
}
