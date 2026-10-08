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
        [SerializeField] private Text title, positionLabel, message;
        [SerializeField] private QuestDropdown orientation;
        [SerializeField] private Slider position;
        [SerializeField] private Toggle flip;
        [SerializeField] private Button minus, plus, remove, applyNormal;
        [SerializeField] private GameObject custom;
        [SerializeField] private Button[] normalFields;
        private readonly string[] draft = new string[3];
        private bool dirty;
        private Base3DScene scene;
        private QuestNumericKeypad keypad;
        public Cut Cut { get; private set; }

        private void Awake()
        {
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
            applyNormal.onClick.AddListener(ApplyNormal);
            for (int i = 0; i < normalFields.Length; i++)
            {
                int axis = i;
                normalFields[i].onClick.AddListener(() => EditNormal(axis));
            }
        }

        public void Bind(Base3DScene source, Cut cut, QuestNumericKeypad pad)
        {
            scene = source;
            Cut = cut;
            keypad = pad;
            Refresh();
        }

        private void Step(int direction)
        {
            if (Cut != null) QuestCutCommands.Position(scene, Cut, Cut.Position + direction / (float)Mathf.Max(1, Cut.NumberOfCuts));
        }

        private void EditNormal(int axis)
        {
            if (!QuestCutCommands.CanEdit(scene, Cut) || keypad == null) return;
            keypad.Open(this, "Normal " + "XYZ"[axis], draft[axis], value =>
            {
                draft[axis] = value;
                dirty = true;
                Refresh();
            });
        }

        public void ApplyNormal()
        {
            if (QuestCutCommands.Normal(scene, Cut, draft[0], draft[1], draft[2]))
            {
                dirty = false;
                message.text = "";
            }
            else message.text = "Enter a finite, nonzero normal (X, Y, Z).";
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (Cut == null) return;
            bool ready = QuestCutCommands.CanEdit(scene, Cut);
            foreach (var control in GetComponentsInChildren<Selectable>(true)) control.interactable = ready;
            title.text = "Cut " + (Cut.Index + 1);
            orientation.SetValueWithoutNotify((int)Cut.Orientation);
            position.SetValueWithoutNotify(Cut.Position);
            positionLabel.text = "Position  " + Cut.Position.ToString("0.000", CultureInfo.InvariantCulture);
            flip.SetIsOnWithoutNotify(Cut.Flip);
            flip.gameObject.SetActive(Cut.Orientation != CutOrientation.Custom);
            custom.SetActive(Cut.Orientation == CutOrientation.Custom);
            if (!dirty)
            {
                draft[0] = Cut.Normal.x.ToString("G7", CultureInfo.InvariantCulture);
                draft[1] = Cut.Normal.y.ToString("G7", CultureInfo.InvariantCulture);
                draft[2] = Cut.Normal.z.ToString("G7", CultureInfo.InvariantCulture);
            }

            for (int i = 0; i < normalFields.Length; i++) normalFields[i].GetComponentInChildren<Text>().text = "XYZ"[i] + "  " + draft[i];
            if (!ready && keypad != null) keypad.CancelFor(this);
        }

        private void OnDisable()
        {
            if (keypad != null) keypad.CancelFor(this);
        }
    }
}
