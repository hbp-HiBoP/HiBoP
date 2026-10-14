using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using HBP.Quest.Desktop;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Quest
{
    public sealed class QuestSendWindow : DialogWindow
    {
        [SerializeField] private Text selection;
        private QuestManager connection;
        private float nextRefresh;

        protected override void Initialize()
        {
            m_OnClose ??= new UnityEvent();
            base.Initialize();
            connection = QuestManager.Instance;
            connection.OnChanged.AddListener(Refresh);
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            if (!this || connection == null) return;
            string error = DesktopSceneCapture.GetSelectionError();
            if (error != null) selection.text = error;
            else
            {
                Base3DScene scene = Module3DMain.SelectedScene;
                int columns = scene.Columns.Count;
                int patients = scene.Visualization.Patients.Count;
                selection.text = $"Scene {scene.Name} {(connection.IsPaired ? "ready to be sent to paired Quest." : "selected. Waiting for paired Quest.")}\n{columns} {(columns == 1 ? "column" : "columns")} · {patients} {(patients == 1 ? "patient" : "patients")}";
            }

            m_OKButton.interactable = connection.IsPaired && !connection.IsBusy && error == null;
        }

        private void OnDestroy()
        {
            if (connection != null) connection.OnChanged.RemoveListener(Refresh);
        }

        public override async void OK()
        {
            if (!m_OKButton.interactable) return;
            m_OKButton.interactable = false;
            Base3DScene scene = Module3DMain.SelectedScene;
            base.OK();
            QuestManager manager = connection;
            // The menu is an explicit recovery request, even if cancellation left no
            // complete retry spool. Display owns its presentation and recovery dialog.
            await manager.SendAsync(true, scene);
        }
    }
}
