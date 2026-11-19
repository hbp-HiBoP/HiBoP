using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.ColorPicker;

namespace HBP.Quest
{
    /// <summary>Quest-sized presentation of the Desktop HSV/RGB control; free changes remain a draft.</summary>
    public sealed class QuestColorPicker : MonoBehaviour
    {
        [SerializeField] private ColorPickerControl control;
        [SerializeField] private Button apply, cancel, reset;
        [SerializeField] private Button[] palette;
        private Action<Color> committed;

        private void Awake()
        {
            apply.onClick.AddListener(() => Commit(control.CurrentColor));
            cancel.onClick.AddListener(Cancel);
            reset.onClick.AddListener(() => Commit(HBP.Core.Object3D.SiteState.DefaultColor));
            foreach (var button in palette)
            {
                var color = button.GetComponent<Image>().color;
                button.onClick.AddListener(() => Commit(color));
            }
        }

        public void Open(Color initial, Action<Color> callback)
        {
            committed = callback;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            control.CurrentColor = initial;
        }

        private void Commit(Color value)
        {
            var callback = committed;
            Cancel();
            callback?.Invoke(value);
        }

        public void Cancel()
        {
            committed = null;
            gameObject.SetActive(false);
        }

        private void OnDisable() => committed = null;
    }
}
