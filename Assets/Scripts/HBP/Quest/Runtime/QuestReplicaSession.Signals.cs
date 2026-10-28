using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Transport;

namespace HBP.Quest
{
    internal sealed partial class QuestReplicaSession
    {
        private readonly SignalBulkReceiver signalBulk = new();
        internal bool SupportsSignals { get; private set; }
        internal SignalPresentationBuffer Signals { get; private set; }
        internal string SignalsError { get; private set; }

        internal SignalPresentationBuffer OpenSignals(long budget)
        {
            CloseSignals();
            SignalsError = null;
            Signals = new SignalPresentationBuffer(Guid.NewGuid(), budget);
            var queued = m_Transport.EnqueueSessionControl(SignalWire.Encode(new SignalMessage { Kind = SignalMessageKind.Subscribe, Generation = Signals.Generation }), DeliveryReliability.Reliable);
            if (!queued.Accepted)
            {
                Signals.ClearData();
                Signals = null;
                throw new IOException("Signals connection is not ready.");
            }

            return Signals;
        }

        internal void CloseSignals()
        {
            if (Signals != null)
            {
                if (!m_Disposed && m_Transport.State == PersistentTransportState.Connected)
                    m_Transport.EnqueueSessionControl(SignalWire.Encode(new SignalMessage { Kind = SignalMessageKind.Unsubscribe, Generation = Signals.Generation }), DeliveryReliability.Reliable);
                Signals.ClearData();
                Signals = null;
            }

            bool abandoned = false;
            foreach (var record in signalBulk.Clear())
            {
                abandoned = true;
                m_Driver.RecordApplicationOrigin(record.MessageId, record.OriginSequence);
                m_ApplicationCompletion.Complete(record.OriginSequence);
            }

            if (abandoned && !m_Disposed && m_Transport.State == PersistentTransportState.Connected) PublishRetentionProgress();
        }

        private async Task ProcessSignalRecordAsync(TransportRecord record, CancellationToken stop)
        {
            ValidateScope(record);
            await UniTask.SwitchToMainThread(stop);
            if (Signals == null)
            {
                if (record.OriginSequence > 0) CompleteApplicationRecord(record);
                return;
            }

            TransportRecord completed = null;
            bool succeeded = true;
            try
            {
                if (!signalBulk.TryReceive(record, out var bytes, out completed) || bytes == null) return;
                var message = SignalWire.Decode(bytes);
                await UniTask.SwitchToMainThread(stop);
                Signals?.Receive(message);
            }
            catch (Exception error) when (error is InvalidDataException || error is EndOfStreamException || error is OverflowException || error is ArgumentException)
            {
                await UniTask.SwitchToMainThread(stop);
                succeeded = false;
                SignalsError = error.Message;
                Signals?.ClearData();
                foreach (var abandoned in signalBulk.Clear()) CompleteApplicationRecord(abandoned);
            }
            finally
            {
                if (completed != null)
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                    if (!m_Disposed)
                    {
                        CompleteApplicationRecord(completed);
                        if (Signals != null)
                            m_Transport.EnqueueSessionControl(SignalWire.Encode(new SignalMessage { Kind = SignalMessageKind.Acknowledge, Generation = Signals.Generation, Key = completed.MessageId.Value.ToString("N"), Total = succeeded ? 0 : 1 }), DeliveryReliability.Reliable);
                    }
                }
            }
        }
    }
}
