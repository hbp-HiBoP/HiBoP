namespace Tools.Unity
{
    public class FolderSelector : PathSelector
    {
        #region Private Methods
        protected override string OpenDialog()
        {
            return HBP.VISU3D.DLL.QtGUI.get_existing_directory_name(Message, Path);
        }
        #endregion
    }
}