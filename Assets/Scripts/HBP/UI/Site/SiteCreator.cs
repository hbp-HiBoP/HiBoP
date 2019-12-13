using Tools.CSharp;
using Tools.Unity.Components;
using UnityEngine.Events;

namespace HBP.UI
{
    public class SiteCreator : ObjectCreator<Data.Site>
    {
        #region Properties
        public GenericEvent<Data.Site> OnTryMergeSite = new GenericEvent<Data.Site>();
        #endregion

        #region Public Methods
        public override void CreateFromFile()
        {
            if (LoadFromFile(out Data.Site[] items))
            {
                foreach (var item in items)
                {
                    OnTryMergeSite.Invoke(item);
                }
            }
        }
        #endregion

        #region Public Methods
        protected override bool LoadFromFile(out Data.Site[] result)
        {
            ILoadable<Data.Site> loadable = new Data.Site() as ILoadable<Data.Site>;
            string path = FileBrowser.GetExistingFileName(loadable.GetExtensions()).StandardizeToPath();
            if (path != string.Empty)
            {
                return loadable.LoadFromFile(path, out result);
            }
            result = new Data.Site[0];
            return false;
        }
        #endregion
    }
}