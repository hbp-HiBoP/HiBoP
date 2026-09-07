using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CRNL.HiBoP.XR.Cuts
{
    public sealed class P12CutGizmo : MonoBehaviour
    {
        public const float MaximumCommandRate = 60f;

        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private MeshRenderer[] feedbackRenderers = Array.Empty<MeshRenderer>();

        private MaterialPropertyBlock m_Properties;
        private SessionEpoch m_Session;
        private ScopeKey m_CutScope;
        private ScopeRevision m_ScopeRevision;
        private ContractId m_InteractionId;
        private InteractionSequence m_Sequence;
        private bool m_Interacting;
        private bool m_Dirty;
        private float m_NextSendTime;
        private Vector3 m_LastPosition;
        private Quaternion m_LastRotation;

        public event Action<Command> CommandRequested;

        public CutFeedbackState Feedback { get; private set; } = CutFeedbackState.Idle;

        public void Configure(XRGrabInteractable interactable, MeshRenderer[] renderers)
        {
            grabInteractable = interactable;
            feedbackRenderers = renderers ?? Array.Empty<MeshRenderer>();
        }

        public void BindContext(SessionEpoch session, ContractId cutId, ScopeRevision scopeRevision)
        {
            if (!session.IsValid || !cutId.IsValid)
                throw new ArgumentException("A valid session and cut ID are required.");
            m_Session = session;
            m_CutScope = new ScopeKey(ScopeType.Cut, cutId);
            m_ScopeRevision = scopeRevision;
            m_InteractionId = default;
            m_Sequence = default;
            m_Interacting = false;
            m_Dirty = false;
            SetFeedback(CutFeedbackState.Canonical);
        }

        public void BeginInteraction()
        {
            EnsureContext();
            m_InteractionId = NewId();
            m_Sequence = default;
            m_Interacting = true;
            m_Dirty = true;
            m_LastPosition = transform.localPosition;
            m_LastRotation = transform.localRotation;
            EmitCurrentPose();
        }

        public void UpdateInteractionNow()
        {
            if (!m_Interacting)
                return;
            m_Dirty = true;
            EmitCurrentPose();
        }

        public void EndInteraction()
        {
            if (!m_Interacting)
                return;
            m_Dirty = true;
            EmitCurrentPose();
            m_Interacting = false;
        }

        public void ApplyCanonical(CutRenderResult result)
        {
            if (result == null || result.CutId != m_CutScope.Id)
                throw new ArgumentException("The canonical result belongs to another cut.", nameof(result));
            m_ScopeRevision = result.CutRevision;
            SetFeedback(CutFeedbackState.Canonical);
        }

        public void SetFeedback(CutFeedbackState feedback)
        {
            Feedback = feedback;
            m_Properties ??= new MaterialPropertyBlock();
            Color color = feedback switch
            {
                CutFeedbackState.Pending => new Color(1f, 0.75f, 0.05f, 0.8f),
                CutFeedbackState.Error => new Color(1f, 0.1f, 0.1f, 0.9f),
                CutFeedbackState.Canonical => new Color(0.1f, 0.85f, 1f, 0.65f),
                _ => new Color(0.6f, 0.6f, 0.6f, 0.5f),
            };
            for (int index = 0; index < feedbackRenderers.Length; index++)
            {
                MeshRenderer renderer = feedbackRenderers[index];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(m_Properties);
                m_Properties.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(m_Properties);
            }
        }

        private void OnEnable()
        {
            if (grabInteractable == null)
                return;
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            if (grabInteractable == null)
                return;
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }

        private void Update()
        {
            if (!m_Interacting)
                return;
            if (transform.localPosition != m_LastPosition || transform.localRotation != m_LastRotation)
            {
                m_LastPosition = transform.localPosition;
                m_LastRotation = transform.localRotation;
                m_Dirty = true;
            }

            if (m_Dirty && Time.unscaledTime >= m_NextSendTime)
                EmitCurrentPose();
        }

        private void EmitCurrentPose()
        {
            if (!m_Dirty)
                return;
            m_Sequence = m_Sequence.IsValid ? m_Sequence.Next() : new InteractionSequence(1);
            Vector3 normal = (transform.localRotation * Vector3.forward).normalized;
            Vector3 pointMillimeters = transform.localPosition * 1000f;
            var intent = new CutPlaneIntent(new Plane3F(new Float3(normal.x, normal.y, normal.z), -Vector3.Dot(normal, pointMillimeters)));
            Command command = CutCommands.Create(m_Session, NewId(), NewId(), m_CutScope, m_ScopeRevision, intent, m_InteractionId, m_Sequence);
            m_Dirty = false;
            m_NextSendTime = Time.unscaledTime + 1f / MaximumCommandRate;
            SetFeedback(CutFeedbackState.Pending);
            CommandRequested?.Invoke(command);
        }

        private void OnSelectEntered(SelectEnterEventArgs _) => BeginInteraction();
        private void OnSelectExited(SelectExitEventArgs _) => EndInteraction();

        private void EnsureContext()
        {
            if (!m_Session.IsValid || !m_CutScope.IsValid)
                throw new InvalidOperationException("Bind a canonical cut context before manipulating the gizmo.");
        }

        private static ContractId NewId()
        {
            return ContractId.FromBytes(Guid.NewGuid().ToByteArray());
        }
    }
}
