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
        [SerializeField] private Button[] normalFields;
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
            var cut = Cut;
            keypad.OpenValidated(this, "Normal " + "XYZ"[axis], cut.Normal[axis].ToString("G7", CultureInfo.InvariantCulture), value =>
            {
                if (Cut != cut || !QuestCutCommands.CanEdit(scene, cut)) return false;
                // Read the other components at confirmation time, including incoming changes.
                var values = new string[3];
                for (int i = 0; i < values.Length; i++) values[i] = cut.Normal[i].ToString("R", CultureInfo.InvariantCulture);
                values[axis] = value;
                bool applied = QuestCutCommands.Normal(scene, cut, values[0], values[1], values[2]);
                Refresh();
                return applied;
            });
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
            flip.gameObject.SetActive(Cut.Orientation != CutOrientation.Custom);
            custom.SetActive(Cut.Orientation == CutOrientation.Custom);
            for (int i = 0; i < normalFields.Length; i++) normalFields[i].GetComponentInChildren<Text>().text = "XYZ"[i] + "  " + Cut.Normal[i].ToString("G7", CultureInfo.InvariantCulture);
            if ((!ready || Cut.Orientation != CutOrientation.Custom) && keypad != null) keypad.CancelFor(this);
        }

        private void OnDisable()
        {
            if (keypad != null) keypad.CancelFor(this);
        }
    }
}
