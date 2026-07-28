using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.UI.Tools
{
    public class DialogBoxManager : Manager<DialogBoxManager>
    {
        #region Properties

        [SerializeField] private GameObject m_DialogBoxPrefab;
        [SerializeField] private GameObject m_ScrollableDialogBoxPrefab;
        [SerializeField] private Canvas m_Canvas;

        #endregion

        #region Public Methods

        public static async UniTaskVoid Open(DialogBoxType type, string title, string message, params string[] buttons)
        {
            await OpenAsync(type, title, message, buttons);
        }

        public static async UniTaskVoid OpenScrollable(DialogBoxType type, string title, string message, params string[] buttons)
        {
            await OpenScrollableAsync(type, title, message, buttons);
        }

        public static async UniTask<int> OpenAsync(DialogBoxType type, string title, string message, params string[] buttons)
        {
            return await OpenAsync(m_Instance.m_DialogBoxPrefab, type, title, message, buttons);
        }

        public static async UniTask<int> OpenScrollableAsync(DialogBoxType type, string title, string message, params string[] buttons)
        {
            return await OpenAsync(m_Instance.m_ScrollableDialogBoxPrefab, type, title, message, buttons);
        }

        #endregion

        #region Private Methods

        private static async UniTask<int> OpenAsync(GameObject prefab, DialogBoxType type, string title, string message, params string[] buttons)
        {
            await UniTask.SwitchToMainThread();
            GameObject dialogBox = Instantiate(prefab, m_Instance.m_Canvas.transform);
            dialogBox.transform.SetAsLastSibling();
            if (buttons.Length == 0) buttons = new string[] { "OK" };
            return await dialogBox.GetComponent<DialogBox>().OpenAsync(type, title, message, buttons);
        }

        #endregion
    }
}
