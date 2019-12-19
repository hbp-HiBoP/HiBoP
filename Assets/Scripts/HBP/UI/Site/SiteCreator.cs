using Tools.CSharp;
using Tools.Unity.Components;

namespace HBP.UI
{
    public class SiteCreator : ObjectCreator<Data.Site>
    {
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