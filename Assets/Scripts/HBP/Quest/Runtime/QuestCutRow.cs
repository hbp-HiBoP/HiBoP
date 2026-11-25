using System.Globalization;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestCutRow : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private QuestDropdown orientation;
        [SerializeField] private Slider position;
        [SerializeField] private Toggle flip;
        [SerializeField] private Button minus, plus, remove;
        [SerializeField] private GameObject custom;
        [SerializeField] private Toggle rotateWithWrist;
        [SerializeField] private QuestTextInputField[] normalFields;
        private Base3DScene scene;
        private QuestCutsPanel panel;
        public Cut Cut { get; private set; }

        private void Awake()
        {
            if (rotateWithWrist != null)
                rotateWithWrist.onValueChanged.AddListener(enabled =>
                {
                    if (Cut != null && Cut.Orientation == CutOrientation.Custom && QuestCutCommands.CanEdit(scene, Cut))
                        panel?.SetWristRotation(Cut, enabled);
                });
            orientation.onValueChanged.AddListener(value =>
            {
                if (Cut != null) QuestCutCommands.Orientation(scene, Cut, (CutOrientation)value);
            });
            position.onValueChanged.AddListener(value =>
            {
                if (Cut != null) QuestCutCommands.Position(scene, Cut, value);
            });
            flip.onValueChanged.AddListener(value =>
            {
                if (Cut != null) QuestCutCommands.Flip(scene, Cut, value);
            });
            minus.onClick.AddListener(() => Step(-1));
            plus.onClick.AddListener(() => Step(1));
            remove.onClick.AddListener(() =>
            {
                if (QuestCutCommands.CanEdit(scene, Cut)) scene.RemoveCutPlane(Cut);
            });
            for (int i = 0; i < normalFields.Length; i++)
            {
                int axis = i;
                normalFields[i].onSubmit.AddListener(value => SetNormal(axis, value));
            }
        }

        public void Bind(Base3DScene source, Cut cut, QuestCutsPanel owner)
        {
            scene = source;
            Cut = cut;
            panel = owner;
            Refresh();
        }

        private void Step(int direction)
        {
            if (Cut != null) QuestCutCommands.Position(scene, Cut, Cut.Position + direction / (float)Mathf.Max(1, Cut.NumberOfCuts));
        }

        private void SetNormal(int axis, string value)
        {
            if (Cut == null || !panel.IsOpen || Cut.Orientation != CutOrientation.Custom || !QuestCutCommands.CanEdit(scene, Cut)) return;
            // Read the other components at confirmation time, including incoming changes.
            var values = new string[3];
            for (int i = 0; i < values.Length; i++) values[i] = Cut.Normal[i].ToString("R", CultureInfo.InvariantCulture);
            values[axis] = value;
            QuestCutCommands.Normal(scene, Cut, values[0], values[1], values[2]);
            Refresh();
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (Cut == null) return;
            bool ready = QuestCutCommands.CanEdit(scene, Cut);
            foreach (var control in GetComponentsInChildren<Selectable>(true)) control.interactable = ready;
            title.text = (Cut.Index + 1).ToString(CultureInfo.InvariantCulture);
            orientation.SetValueWithoutNotify((int)Cut.Orientation);
            position.SetValueWithoutNotify(Cut.Position);
            flip.SetIsOnWithoutNotify(Cut.Flip);
            bool isCustom = Cut.Orientation == CutOrientation.Custom;
            flip.gameObject.SetActive(!isCustom);
            custom.SetActive(isCustom);
            if (rotateWithWrist != null)
            {
                rotateWithWrist.gameObject.SetActive(isCustom);
                rotateWithWrist.SetIsOnWithoutNotify(panel?.IsWristRotationEnabled(Cut) == true);
            }

            for (int i = 0; i < normalFields.Length; i++)
            {
                var field = normalFields[i];
                if (!ready || !isCustom || !panel.IsOpen) field.CancelEditing();
                if (!field.IsEditing) field.SetTextWithoutNotify(QuestTextInputField.FormatDecimal(Cut.Normal[i]));
            }
        }
    }
}
