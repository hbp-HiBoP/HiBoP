namespace Tools.Unity
{
    public class FileSelector : PathSelector
    {
        #region Properties
        public string Extension;
        #endregion

        #region Private Methods
        protected override string OpenDialog()
        {
            return HBP.VISU3D.DLL.QtGUI.get_existing_file_name(new string[] { Extension }, Message, Path);
        }
        #endregion
    }
}

