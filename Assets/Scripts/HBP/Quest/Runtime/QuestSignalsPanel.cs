using System;
using System.Linq;
using HBP.Data.Module3D;
using HBP.Sync;
using UnityEngine;
using UnityEngine.UI;


namespace HBP.Quest

{
    public sealed class QuestSignalsPanel : MonoBehaviour

    {
        [SerializeField] private QuestAnatomyView view;

        [SerializeField] private QuestAnatomySession session;

        [SerializeField] private QuestWindow window;

        [SerializeField] private Text identity, status;

        [SerializeField] private QuestSignalGraphView graph;

        [SerializeField] private QuestSignalMatrixGrid matrixGrid;

        [SerializeField] private Button retry;

        [SerializeField, Range(16, 128)] private int memoryBudgetMiB = 128;

        private QuestReplicaSession replica;

        private Base3DScene scene;

        private SignalPresentationBuffer buffer;

        private SignalSnapshot displayed;

        private string failure;


        private float responseDeadline;
        private bool receiving;

        private float capabilityDeadline;


        private void Awake()

        {
            retry.onClick.AddListener(() =>
            {
                Unbind();
                failure = null;
            });

            window.OnChanged.AddListener(OnWindowChanged);

            SetStatus("Connect to Desktop to view signals.");
        }

        private void OnWindowChanged(QuestWindow value)
        {
            if (!value.IsOpen)
            {
                Unbind();
                failure = null;
            }
        }

        private void Update()

        {
            if (!window.IsOpen) return;

            var selected = view != null ? view.Scene : null;

            var available = selected != null && session != null ? session.SignalsReplica(selected) : null;

            if (scene != selected || replica != available)

            {
                Unbind();
                scene = selected;
                failure = null;
                identity.supportRichText = false;
                identity.text = selected != null ? selected.Visualization.Name : "Select a scene.";

                if (available != null)

                {
                    try

                    {
                        replica = available;

                        capabilityDeadline = Time.unscaledTime + 5;

                        if (!replica.SupportsSignals)
                        {
                            SetStatus("Waiting for Desktop…");
                            return;
                        }

                        buffer = replica.OpenSignals(memoryBudgetMiB * 1024L * 1024);
                        receiving = true;
                        responseDeadline = Time.unscaledTime + 15;

                        buffer.Progress += OnProgress;
                        buffer.Loading += OnLoading;
                        buffer.Published += Publish;
                        buffer.Failed += Fail;

                        SetStatus("Receiving signals…");
                    }

                    catch (Exception error)
                    {
                        Fail(error.Message);
                    }
                }
            }

            if (available != null && buffer == null && failure == null)

            {
                if (available.SupportsSignals)
                {
                    replica = null;
                    return;
                }

                if (Time.unscaledTime > capabilityDeadline) Fail("Update Desktop and Quest to use Signals. The scene connection remains available.");
            }

            if (buffer != null && receiving && failure == null && Time.unscaledTime > responseDeadline) Fail("Signals transfer timed out. Retry from this window.");

            if (available == null) SetStatus("Connect to Desktop to view signals.");

            if (replica?.SignalsError != null && failure == null) Fail(replica.SignalsError);

            retry.gameObject.SetActive(failure != null && available != null);

            UpdateCursor();
        }

        private void OnProgress() => responseDeadline = Time.unscaledTime + 15;

        private void OnLoading(bool content)

        {
            receiving = true;
            failure = null;
            responseDeadline = Time.unscaledTime + 15;

            if (content) ClearVisuals();

            SetStatus(content ? "Receiving signals…" : "Updating signals…");
        }

        private void Publish(SignalSnapshot snapshot)

        {
            try

            {
                receiving = false;
                displayed = snapshot;
                identity.supportRichText = false;
                identity.text = snapshot.Title;

                graph.Bind(snapshot.Graphs.Length == 0 ? null : snapshot.Graphs[snapshot.ActiveGraph], buffer.Resources);
                matrixGrid.Bind(snapshot, buffer.Resources);

                SetStatus(snapshot.Graphs.Length == 0 && snapshot.Matrices.Length == 0 ? "No signals for the displayed sites." : "");
            }

            catch (Exception error) when (error is ArgumentException || error is System.IO.InvalidDataException || error is OverflowException)
            {
                Fail(error.Message);
            }
        }

        private void UpdateCursor()

        {
            bool visible = false;
            float currentTime = 0;

            if (displayed != null && displayed.Graphs.Length > 0 && scene != null && scene.IsGeneratorUpToDate)

            {
                var active = displayed.Graphs[displayed.ActiveGraph];

                for (int i = 0; i < active.ColumnIds.Length; i++)

                {
                    var column = scene.Columns.FirstOrDefault(c => c.ColumnData.ID == active.ColumnIds[i]) as Column3DDynamic;

                    if (column == null || !column.IsSelected || column.IsMinimized) continue;

                    var sub = column.Timeline.SubTimelinesBySubBloc.FirstOrDefault(pair => pair.Key.ID == active.SubBlocIds[i]);

                    if (sub.Key == null || sub.Value != column.Timeline.CurrentSubtimeline) continue;

                    float time = column.Timeline.CurrentSubtimeline.GetLocalTime(column.Timeline.CurrentIndex);

                    float x = (time - active.XMin) / Mathf.Max(.0001f, active.XMax - active.XMin);

                    currentTime = time;
                    visible = x >= 0 && x <= 1;
                    break;
                }
            }

            graph.SetTime(currentTime, visible);
        }

        private void SetStatus(string message)
        {
            status.text = message;
            status.transform.parent.gameObject.SetActive(message.Length > 0);
        }

        private void Fail(string message)
        {
            receiving = false;
            failure = message;
            ClearVisuals();
            buffer?.ClearData();
            SetStatus(message);
        }

        private void ClearVisuals()

        {
            displayed = null;
            if (graph != null) graph.Clear();
            if (matrixGrid != null) matrixGrid.Clear();
        }

        private void Unbind()

        {
            if (buffer != null)
            {
                buffer.Progress -= OnProgress;
                buffer.Loading -= OnLoading;
                buffer.Published -= Publish;
                buffer.Failed -= Fail;
            }

            replica?.CloseSignals();
            buffer?.ClearData();
            replica = null;
            buffer = null;
            scene = null;
            ClearVisuals();
        }

        private void OnDestroy()
        {
            Unbind();
            if (window != null) window.OnChanged.RemoveListener(OnWindowChanged);
        }
    }
}
