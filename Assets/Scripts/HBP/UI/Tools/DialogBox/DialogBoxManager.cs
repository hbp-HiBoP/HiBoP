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
        [SerializeField] private Canvas m_Canvas;
        #endregion

        #region Public Methods
        public static async UniTaskVoid Open(DialogBoxType type, string title, string message, params string[] buttons)
        {
            await OpenAsync(type, title, message, buttons);
        }
        public static async UniTask<int> OpenAsync(DialogBoxType type, string title, string message, params string[] buttons)
        {
            GameObject dialogBox = Instantiate(m_Instance.m_DialogBoxPrefab, m_Instance.m_Canvas.transform);
            dialogBox.transform.SetAsLastSibling();
            if (buttons.Length == 0) buttons = new string[] { "OK" };
            return await dialogBox.GetComponent<DialogBox>().OpenAsync(type, title, message, buttons);
        }
        #endregion
    }
}