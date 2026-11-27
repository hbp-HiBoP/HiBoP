using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Sync
{
    public enum SceneBooleanProperty : byte
    {
        StrongCuts = 1,
        HideBlacklistedSites = 2,
        EdgeMode = 3,
        BrainTransparent = 4,
        DisplayMarsAtlas = 5,
        DisplayJuBrainAtlas = 6,
        AutomaticCutAroundSelectedSite = 7,
        ShowAllSites = 8,
        Visible = 9
    }

    public enum SceneFloatProperty : byte
    {
        SiteGain = 1,
        BrainAlpha = 2,
        AtlasAlpha = 3
    }

    public enum SceneColorProperty : byte
    {
        Brain = 1,
        Cut = 2,
        Colormap = 3
    }

    public enum ColumnSpanKind : byte
    {
        Static = 1,
        Dynamic = 2,

        // Density payloads carry ratios of the fixed reference maximum, never absolute values.
        Anatomy = 3
    }

    public enum FunctionalModality : byte
    {
        Fmri = 1,
        Meg = 2
    }

    public sealed class SetSelectedColumn : Mutation
    {
        public ColumnId ColumnId { get; }
        public override OperationType Type => OperationType.SetSelectedColumn;
        public SetSelectedColumn(ColumnId columnId) => ColumnId = columnId;
    }

    public sealed class SetSelectedSite : Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public override IReadOnlyList<SetCutDefinition> ReplacementCuts { get; }
        public override OperationType Type => OperationType.SetSelectedSite;

        public SetSelectedSite(ColumnId columnId, SiteId siteId, IEnumerable<SetCutDefinition> replacementCuts = null)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId;
            ReplacementCuts = ScenePresentationMutationCodec.ValidateReplacementCuts(replacementCuts);
        }
    }

    public sealed class SetSceneBoolean : Mutation
    {
        public SceneBooleanProperty Property { get; }
        public bool Value { get; }
        public override IReadOnlyList<SetCutDefinition> ReplacementCuts { get; }
        public override OperationType Type => OperationType.SetSceneBoolean;

        public SetSceneBoolean(SceneBooleanProperty property, bool value, IEnumerable<SetCutDefinition> replacementCuts = null)
        {
            if ((byte)property < (byte)SceneBooleanProperty.StrongCuts || (byte)property > (byte)SceneBooleanProperty.Visible) throw new ArgumentOutOfRangeException(nameof(property));
            Property = property;
            Value = value;
            if (replacementCuts != null && property != SceneBooleanProperty.AutomaticCutAroundSelectedSite)
                throw new ArgumentException("Only the automatic cut policy can replace cuts.", nameof(replacementCuts));
            ReplacementCuts = ScenePresentationMutationCodec.ValidateReplacementCuts(replacementCuts);
        }
    }

    public sealed class SetSceneFloat : Mutation
    {
        public SceneFloatProperty Property { get; }
        public float Value { get; }
        public override OperationType Type => OperationType.SetSceneFloat;

        public SetSceneFloat(SceneFloatProperty property, float value)
        {
            if ((byte)property < (byte)SceneFloatProperty.SiteGain || (byte)property > (byte)SceneFloatProperty.AtlasAlpha) throw new ArgumentOutOfRangeException(nameof(property));
            ValueValidation.ValidateFloat(value, nameof(value));
            if (property != SceneFloatProperty.SiteGain && (value < 0f || value > 1f)) throw new ArgumentOutOfRangeException(nameof(value));
            Property = property;
            Value = value;
        }
    }

    public sealed class SetSceneColor : Mutation
    {
        public SceneColorProperty Property { get; }
        public int Value { get; }
        public override OperationType Type => OperationType.SetSceneColor;

        public SetSceneColor(SceneColorProperty property, int value)
        {
            if ((byte)property < (byte)SceneColorProperty.Brain || (byte)property > (byte)SceneColorProperty.Colormap) throw new ArgumentOutOfRangeException(nameof(property));
            if (value < 0 || value > 17) throw new ArgumentOutOfRangeException(nameof(value));
            Property = property;
            Value = value;
        }
    }

    public sealed class SetSiteHighlight : Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public bool Highlighted { get; }
        public override OperationType Type => OperationType.SetSiteHighlight;

        public SetSiteHighlight(ColumnId columnId, SiteId siteId, bool highlighted)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            Highlighted = highlighted;
        }
    }

    public sealed class SetSiteLabels : Mutation
    {
        private readonly string[] m_Labels;
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public IReadOnlyList<string> Labels => Array.AsReadOnly(m_Labels);
        public override OperationType Type => OperationType.SetSiteLabels;

        public SetSiteLabels(ColumnId columnId, SiteId siteId, IEnumerable<string> labels)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            if (labels == null) throw new ArgumentNullException(nameof(labels));
            m_Labels = labels.ToArray();
            if (m_Labels.Length > 128) throw new ArgumentOutOfRangeException(nameof(labels));
            int bytes = 0;
            for (int i = 0; i < m_Labels.Length; i++)
            {
                m_Labels[i] = MutationValueValidation.ValidateRequiredText(m_Labels[i], nameof(labels));
                int length = MutationValueValidation.StrictUtf8.GetByteCount(m_Labels[i]);
                bytes = checked(bytes + 2 + length);
            }

            if (bytes > 384) throw new ArgumentOutOfRangeException(nameof(labels), "Ordered labels exceed the bounded mutation payload.");
        }
    }

    public sealed class SetActivityAlpha : Mutation
    {
        public ColumnId ColumnId { get; }
        public float Alpha { get; }
        public override OperationType Type => OperationType.SetActivityAlpha;

        public SetActivityAlpha(ColumnId columnId, float alpha)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            MutationValueValidation.ValidateRange(alpha, 0f, 1f, nameof(alpha));
            Alpha = alpha;
        }
    }

    public sealed class SetColumnSpan : Mutation
    {
        public ColumnId ColumnId { get; }
        public ColumnSpanKind Kind { get; }
        public float Minimum { get; }
        public float Middle { get; }
        public float Maximum { get; }
        public override OperationType Type => OperationType.SetColumnSpan;

        public SetColumnSpan(ColumnId columnId, ColumnSpanKind kind, float minimum, float middle, float maximum)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (kind != ColumnSpanKind.Static && kind != ColumnSpanKind.Dynamic && kind != ColumnSpanKind.Anatomy) throw new ArgumentOutOfRangeException(nameof(kind));
            ValueValidation.ValidateFloat(minimum, nameof(minimum));
            ValueValidation.ValidateFloat(middle, nameof(middle));
            ValueValidation.ValidateFloat(maximum, nameof(maximum));
            if (minimum > middle || middle > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (kind == ColumnSpanKind.Anatomy && minimum < 0f) throw new ArgumentOutOfRangeException(nameof(minimum));
            Kind = kind;
            Minimum = minimum;
            Middle = middle;
            Maximum = maximum;
        }
    }

    public sealed class SetFunctionalDisplay : Mutation
    {
        public ColumnId ColumnId { get; }
        public FunctionalModality Modality { get; }
        public float NegativeMinimum { get; }
        public float NegativeMaximum { get; }
        public float PositiveMinimum { get; }
        public float PositiveMaximum { get; }
        public bool HideLower { get; }
        public bool HideMiddle { get; }
        public bool HideHigher { get; }
        public override OperationType Type => OperationType.SetFunctionalDisplay;

        public SetFunctionalDisplay(ColumnId columnId, FunctionalModality modality, float negativeMinimum, float negativeMaximum, float positiveMinimum, float positiveMaximum, bool hideLower, bool hideMiddle, bool hideHigher)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (modality != FunctionalModality.Fmri && modality != FunctionalModality.Meg) throw new ArgumentOutOfRangeException(nameof(modality));
            MutationValueValidation.ValidateRange(negativeMinimum, 0f, 1f, nameof(negativeMinimum));
            MutationValueValidation.ValidateRange(negativeMaximum, negativeMinimum, 1f, nameof(negativeMaximum));
            MutationValueValidation.ValidateRange(positiveMinimum, 0f, 1f, nameof(positiveMinimum));
            MutationValueValidation.ValidateRange(positiveMaximum, positiveMinimum, 1f, nameof(positiveMaximum));
            Modality = modality;
            NegativeMinimum = negativeMinimum;
            NegativeMaximum = negativeMaximum;
            PositiveMinimum = positiveMinimum;
            PositiveMaximum = positiveMaximum;
            HideLower = hideLower;
            HideMiddle = hideMiddle;
            HideHigher = hideHigher;
        }
    }

    public sealed class SetIbcDifumoDisplay : Mutation
    {
        public bool IbcEnabled { get; }
        public string IbcContrastReference { get; }
        public bool DifumoEnabled { get; }
        public string DifumoAtlasReference { get; }
        public int DifumoArea { get; }
        public override OperationType Type => OperationType.SetIbcDifumoDisplay;

        public SetIbcDifumoDisplay(bool ibcEnabled, string ibcContrastReference, bool difumoEnabled, string difumoAtlasReference, int difumoArea)
        {
            IbcContrastReference = MutationValueValidation.ValidateOptionalText(ibcContrastReference, nameof(ibcContrastReference));
            DifumoAtlasReference = MutationValueValidation.ValidateOptionalText(difumoAtlasReference, nameof(difumoAtlasReference));
            if (difumoArea < 0) throw new ArgumentOutOfRangeException(nameof(difumoArea));
            if (ibcEnabled && IbcContrastReference.Length == 0 || difumoEnabled && DifumoAtlasReference.Length == 0) throw new ArgumentException("An enabled atlas source requires a prepared-resource reference.");
            IbcEnabled = ibcEnabled;
            DifumoEnabled = difumoEnabled;
            DifumoArea = difumoArea;
        }
    }

    public sealed class SetLocalizerDisplay : Mutation
    {
        public bool Enabled { get; }
        public string ProtocolReference { get; }
        public string DataReference { get; }
        public string BlocReference { get; }
        public int TimelineIndex { get; }
        public float Minimum { get; }
        public float Middle { get; }
        public float Maximum { get; }
        public override OperationType Type => OperationType.SetLocalizerDisplay;

        public SetLocalizerDisplay(bool enabled, string protocolReference, string dataReference, string blocReference, int timelineIndex, float minimum, float middle, float maximum)
        {
            ProtocolReference = MutationValueValidation.ValidateOptionalText(protocolReference, nameof(protocolReference));
            DataReference = MutationValueValidation.ValidateOptionalText(dataReference, nameof(dataReference));
            BlocReference = MutationValueValidation.ValidateOptionalText(blocReference, nameof(blocReference));
            if (timelineIndex < 0) throw new ArgumentOutOfRangeException(nameof(timelineIndex));
            ValueValidation.ValidateFloat(minimum, nameof(minimum));
            ValueValidation.ValidateFloat(middle, nameof(middle));
            ValueValidation.ValidateFloat(maximum, nameof(maximum));
            if (minimum > middle || middle > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (enabled && (ProtocolReference.Length == 0 || DataReference.Length == 0 || BlocReference.Length == 0)) throw new ArgumentException("An enabled localizer requires a prepared protocol, data set and bloc.");
            Enabled = enabled;
            TimelineIndex = timelineIndex;
            Minimum = minimum;
            Middle = middle;
            Maximum = maximum;
        }
    }

    public sealed class SetFmriAtlasCalibration : Mutation
    {
        public float Alpha { get; }
        public float NegativeMinimum { get; }
        public float NegativeMaximum { get; }
        public float PositiveMinimum { get; }
        public float PositiveMaximum { get; }
        public override OperationType Type => OperationType.SetFmriAtlasCalibration;

        public SetFmriAtlasCalibration(float alpha, float negativeMinimum, float negativeMaximum, float positiveMinimum, float positiveMaximum)
        {
            MutationValueValidation.ValidateRange(alpha, 0f, 1f, nameof(alpha));
            MutationValueValidation.ValidateRange(negativeMinimum, 0f, 1f, nameof(negativeMinimum));
            MutationValueValidation.ValidateRange(negativeMaximum, negativeMinimum, 1f, nameof(negativeMaximum));
            MutationValueValidation.ValidateRange(positiveMinimum, 0f, 1f, nameof(positiveMinimum));
            MutationValueValidation.ValidateRange(positiveMaximum, positiveMinimum, 1f, nameof(positiveMaximum));
            Alpha = alpha;
            NegativeMinimum = negativeMinimum;
            NegativeMaximum = negativeMaximum;
            PositiveMinimum = positiveMinimum;
            PositiveMaximum = positiveMaximum;
        }
    }

    public sealed class SetSelectedRoiSphere : Mutation
    {
        public string RoiId { get; }
        public string SphereId { get; }
        public override OperationType Type => OperationType.SetSelectedRoiSphere;

        public SetSelectedRoiSphere(string roiId, string sphereId)
        {
            RoiId = MutationValueValidation.ValidateRequiredText(roiId, nameof(roiId));
            SphereId = MutationValueValidation.ValidateOptionalText(sphereId, nameof(sphereId));
        }
    }

    public abstract class ScenePresentationCheckpointRecord
    {
        public abstract Mutation Value { get; }
        public ushort RecordType => (ushort)Value.Type;
        public ushort SchemaVersion => 1;
        public byte[] Encode() => CheckpointRecordCodec.Encode(RecordType, Value);

        public static ScenePresentationCheckpointRecord FromMutation(Mutation mutation) =>
            mutation switch
            {
                SetSelectedColumn value => new SelectedColumnCheckpointRecord(value),
                SetSelectedSite value => new SelectedSiteCheckpointRecord(value),
                SetSceneBoolean value => new SceneBooleanCheckpointRecord(value),
                SetSceneFloat value => new SceneFloatCheckpointRecord(value),
                SetSceneColor value => new SceneColorCheckpointRecord(value),
                SetSiteHighlight value => new SiteHighlightCheckpointRecord(value),
                SetSiteLabels value => new SiteLabelsCheckpointRecord(value),
                SetActivityAlpha value => new ActivityAlphaCheckpointRecord(value),
                SetColumnSpan value => new ColumnSpanCheckpointRecord(value),
                SetFunctionalDisplay value => new FunctionalDisplayCheckpointRecord(value),
                SetIbcDifumoDisplay value => new IbcDifumoDisplayCheckpointRecord(value),
                SetLocalizerDisplay value => new LocalizerDisplayCheckpointRecord(value),
                SetFmriAtlasCalibration value => new FmriAtlasCalibrationCheckpointRecord(value),
                SetSelectedRoiSphere value => new SelectedRoiSphereCheckpointRecord(value),
                _ => throw new ArgumentException("Mutation is not part of the scene presentation contract.", nameof(mutation))
            };

        public static ScenePresentationCheckpointRecord Decode(byte[] bytes)
        {
            Mutation value = CheckpointRecordCodec.DecodeAny(bytes);
            return FromMutation(value);
        }
    }

    public abstract class ScenePresentationCheckpointRecord<T> : ScenePresentationCheckpointRecord where T : Mutation
    {
        public T TypedValue { get; }
        public sealed override Mutation Value => TypedValue;
        protected ScenePresentationCheckpointRecord(T value) => TypedValue = value ?? throw new ArgumentNullException(nameof(value));
    }

    public sealed class SelectedColumnCheckpointRecord : ScenePresentationCheckpointRecord<SetSelectedColumn>
    {
        public SelectedColumnCheckpointRecord(SetSelectedColumn value) : base(value)
        {
        }
    }

    public sealed class SelectedSiteCheckpointRecord : ScenePresentationCheckpointRecord<SetSelectedSite>
    {
        public SelectedSiteCheckpointRecord(SetSelectedSite value) : base(value)
        {
        }
    }

    public sealed class SceneBooleanCheckpointRecord : ScenePresentationCheckpointRecord<SetSceneBoolean>
    {
        public SceneBooleanCheckpointRecord(SetSceneBoolean value) : base(value)
        {
        }
    }

    public sealed class SceneFloatCheckpointRecord : ScenePresentationCheckpointRecord<SetSceneFloat>
    {
        public SceneFloatCheckpointRecord(SetSceneFloat value) : base(value)
        {
        }
    }

    public sealed class SceneColorCheckpointRecord : ScenePresentationCheckpointRecord<SetSceneColor>
    {
        public SceneColorCheckpointRecord(SetSceneColor value) : base(value)
        {
        }
    }

    public sealed class SiteHighlightCheckpointRecord : ScenePresentationCheckpointRecord<SetSiteHighlight>
    {
        public SiteHighlightCheckpointRecord(SetSiteHighlight value) : base(value)
        {
        }
    }

    public sealed class SiteLabelsCheckpointRecord : ScenePresentationCheckpointRecord<SetSiteLabels>
    {
        public SiteLabelsCheckpointRecord(SetSiteLabels value) : base(value)
        {
        }
    }

    public sealed class ActivityAlphaCheckpointRecord : ScenePresentationCheckpointRecord<SetActivityAlpha>
    {
        public ActivityAlphaCheckpointRecord(SetActivityAlpha value) : base(value)
        {
        }
    }

    public sealed class ColumnSpanCheckpointRecord : ScenePresentationCheckpointRecord<SetColumnSpan>
    {
        public ColumnSpanCheckpointRecord(SetColumnSpan value) : base(value)
        {
        }
    }

    public sealed class FunctionalDisplayCheckpointRecord : ScenePresentationCheckpointRecord<SetFunctionalDisplay>
    {
        public FunctionalDisplayCheckpointRecord(SetFunctionalDisplay value) : base(value)
        {
        }
    }

    public sealed class IbcDifumoDisplayCheckpointRecord : ScenePresentationCheckpointRecord<SetIbcDifumoDisplay>
    {
        public IbcDifumoDisplayCheckpointRecord(SetIbcDifumoDisplay value) : base(value)
        {
        }
    }

    public sealed class LocalizerDisplayCheckpointRecord : ScenePresentationCheckpointRecord<SetLocalizerDisplay>
    {
        public LocalizerDisplayCheckpointRecord(SetLocalizerDisplay value) : base(value)
        {
        }
    }

    public sealed class FmriAtlasCalibrationCheckpointRecord : ScenePresentationCheckpointRecord<SetFmriAtlasCalibration>
    {
        public FmriAtlasCalibrationCheckpointRecord(SetFmriAtlasCalibration value) : base(value)
        {
        }
    }

    public sealed class SelectedRoiSphereCheckpointRecord : ScenePresentationCheckpointRecord<SetSelectedRoiSphere>
    {
        public SelectedRoiSphereCheckpointRecord(SetSelectedRoiSphere value) : base(value)
        {
        }
    }

    internal static class MutationValueValidation
    {
        internal static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static void ValidateRange(float value, float minimum, float maximum, string name)
        {
            ValueValidation.ValidateFloat(value, name);
            if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
        }

        internal static string ValidateRequiredText(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Text identity must not be empty.", name);
            return ValidateOptionalText(value, name);
        }

        internal static string ValidateOptionalText(string value, string name)
        {
            if (value == null) throw new ArgumentNullException(name);
            string normalized;
            try
            {
                normalized = value.Normalize(NormalizationForm.FormC);
                if (StrictUtf8.GetByteCount(normalized) > 256) throw new ArgumentOutOfRangeException(name);
            }
            catch (ArgumentException exception) when (!(exception is ArgumentOutOfRangeException))
            {
                throw new ArgumentException("Text must contain valid Unicode.", name, exception);
            }

            foreach (char character in normalized)
                if (char.IsControl(character))
                    throw new ArgumentException("Text must not contain control characters.", name);
            return normalized;
        }
    }

    internal static class ScenePresentationMutationCodec
    {
        internal static IReadOnlyList<SetCutDefinition> ValidateReplacementCuts(IEnumerable<SetCutDefinition> cuts)
        {
            if (cuts == null) return null;
            var definitions = cuts.ToArray();
            if (definitions.Length > 256 || definitions.Any(cut => cut == null) || definitions.Select(cut => cut.CutId).Distinct().Count() != definitions.Length)
                throw new ArgumentException("Replacement cuts must contain at most 256 unique, supported definitions.", nameof(cuts));
            return Array.AsReadOnly(definitions);
        }

        private static IEnumerable<SetCutDefinition> ReadReplacementCuts(BinaryReader reader)
        {
            if (reader.BaseStream.Position == reader.BaseStream.Length) return null;
            int count = reader.ReadUInt16();
            if (count > 256) throw new InvalidDataException("Replacement cuts exceed their bound.");
            return Enumerable.Range(0, count).Select(_ => SceneStructureMutationCodec.ReadCutDefinition(reader)).ToArray();
        }

        private static void WriteReplacementCuts(BinaryWriter writer, IReadOnlyList<SetCutDefinition> cuts)
        {
            if (cuts == null) return;
            writer.Write((ushort)cuts.Count);
            foreach (var cut in cuts) SceneStructureMutationCodec.WriteCompleteCutDefinition(writer, cut);
        }

        internal static Mutation ReadBody(BinaryReader reader, OperationType type)
        {
            switch (type)
            {
                case OperationType.SetSelectedColumn:
                    return new SetSelectedColumn(ReadOptionalColumn(reader));
                case OperationType.SetSelectedSite:
                    return new SetSelectedSite(ReadColumn(reader), ReadOptionalSite(reader), ReadReplacementCuts(reader));
                case OperationType.SetSceneBoolean:
                    return new SetSceneBoolean(ReadEnum<SceneBooleanProperty>(reader), ReadBoolean(reader), ReadReplacementCuts(reader));
                case OperationType.SetSceneFloat:
                    return new SetSceneFloat(ReadEnum<SceneFloatProperty>(reader), reader.ReadSingle());
                case OperationType.SetSceneColor:
                    return new SetSceneColor(ReadEnum<SceneColorProperty>(reader), reader.ReadInt32());
                case OperationType.SetSiteHighlight:
                    return new SetSiteHighlight(ReadColumn(reader), ReadSite(reader), ReadBoolean(reader));
                case OperationType.SetSiteLabels:
                    {
                        ColumnId column = ReadColumn(reader);
                        SiteId site = ReadSite(reader);
                        ushort count = reader.ReadUInt16();
                        if (count > 128) throw new InvalidDataException("Too many site labels.");
                        var labels = new string[count];
                        for (int i = 0; i < labels.Length; i++) labels[i] = ReadText(reader, allowEmpty: false);
                        return new SetSiteLabels(column, site, labels);
                    }
                case OperationType.SetActivityAlpha:
                    return new SetActivityAlpha(ReadColumn(reader), reader.ReadSingle());
                case OperationType.SetColumnSpan:
                    return new SetColumnSpan(ReadColumn(reader), ReadEnum<ColumnSpanKind>(reader), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case OperationType.SetFunctionalDisplay:
                    {
                        ColumnId column = ReadColumn(reader);
                        FunctionalModality modality = ReadEnum<FunctionalModality>(reader);
                        float negativeMin = reader.ReadSingle();
                        float negativeMax = reader.ReadSingle();
                        float positiveMin = reader.ReadSingle();
                        float positiveMax = reader.ReadSingle();
                        return new SetFunctionalDisplay(column, modality, negativeMin, negativeMax, positiveMin, positiveMax, ReadBoolean(reader), ReadBoolean(reader), ReadBoolean(reader));
                    }
                case OperationType.SetIbcDifumoDisplay:
                    return new SetIbcDifumoDisplay(ReadBoolean(reader), ReadText(reader), ReadBoolean(reader), ReadText(reader), reader.ReadInt32());
                case OperationType.SetLocalizerDisplay:
                    return new SetLocalizerDisplay(ReadBoolean(reader), ReadText(reader), ReadText(reader), ReadText(reader), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case OperationType.SetFmriAtlasCalibration:
                    return new SetFmriAtlasCalibration(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case OperationType.SetSelectedRoiSphere:
                    return new SetSelectedRoiSphere(ReadText(reader, allowEmpty: false), ReadText(reader));
                default:
                    throw new InvalidDataException("Unsupported scene presentation mutation type.");
            }
        }

        internal static void WriteBody(BinaryWriter writer, Mutation mutation)
        {
            switch (mutation)
            {
                case SetSelectedColumn value: WriteOptionalText(writer, value.ColumnId?.Value); break;
                case SetSelectedSite value:
                    WriteText(writer, value.ColumnId.Value);
                    WriteOptionalText(writer, value.SiteId?.Value);
                    WriteReplacementCuts(writer, value.ReplacementCuts);
                    break;
                case SetSceneBoolean value:
                    writer.Write((byte)value.Property);
                    writer.Write((byte)(value.Value ? 1 : 0));
                    WriteReplacementCuts(writer, value.ReplacementCuts);
                    break;
                case SetSceneFloat value:
                    writer.Write((byte)value.Property);
                    writer.Write(value.Value);
                    break;
                case SetSceneColor value:
                    writer.Write((byte)value.Property);
                    writer.Write(value.Value);
                    break;
                case SetSiteHighlight value:
                    WriteText(writer, value.ColumnId.Value);
                    WriteText(writer, value.SiteId.Value);
                    writer.Write((byte)(value.Highlighted ? 1 : 0));
                    break;
                case SetSiteLabels value:
                    WriteText(writer, value.ColumnId.Value);
                    WriteText(writer, value.SiteId.Value);
                    writer.Write(checked((ushort)value.Labels.Count));
                    foreach (string label in value.Labels) WriteText(writer, label);
                    break;
                case SetActivityAlpha value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write(value.Alpha);
                    break;
                case SetColumnSpan value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write((byte)value.Kind);
                    writer.Write(value.Minimum);
                    writer.Write(value.Middle);
                    writer.Write(value.Maximum);
                    break;
                case SetFunctionalDisplay value:
                    WriteText(writer, value.ColumnId.Value);
                    writer.Write((byte)value.Modality);
                    writer.Write(value.NegativeMinimum);
                    writer.Write(value.NegativeMaximum);
                    writer.Write(value.PositiveMinimum);
                    writer.Write(value.PositiveMaximum);
                    writer.Write((byte)(value.HideLower ? 1 : 0));
                    writer.Write((byte)(value.HideMiddle ? 1 : 0));
                    writer.Write((byte)(value.HideHigher ? 1 : 0));
                    break;
                case SetIbcDifumoDisplay value:
                    writer.Write((byte)(value.IbcEnabled ? 1 : 0));
                    WriteText(writer, value.IbcContrastReference);
                    writer.Write((byte)(value.DifumoEnabled ? 1 : 0));
                    WriteText(writer, value.DifumoAtlasReference);
                    writer.Write(value.DifumoArea);
                    break;
                case SetLocalizerDisplay value:
                    writer.Write((byte)(value.Enabled ? 1 : 0));
                    WriteText(writer, value.ProtocolReference);
                    WriteText(writer, value.DataReference);
                    WriteText(writer, value.BlocReference);
                    writer.Write(value.TimelineIndex);
                    writer.Write(value.Minimum);
                    writer.Write(value.Middle);
                    writer.Write(value.Maximum);
                    break;
                case SetFmriAtlasCalibration value:
                    writer.Write(value.Alpha);
                    writer.Write(value.NegativeMinimum);
                    writer.Write(value.NegativeMaximum);
                    writer.Write(value.PositiveMinimum);
                    writer.Write(value.PositiveMaximum);
                    break;
                case SetSelectedRoiSphere value:
                    WriteText(writer, value.RoiId);
                    WriteText(writer, value.SphereId);
                    break;
                default: throw new ArgumentException("Unsupported scene presentation mutation type.", nameof(mutation));
            }
        }

        private static T ReadEnum<T>(BinaryReader reader) where T : struct, Enum
        {
            byte value = reader.ReadByte();
            if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException("Invalid scene presentation enum value.");
            return (T)Enum.ToObject(typeof(T), value);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid scene presentation boolean value.");
            return value == 1;
        }

        private static ColumnId ReadColumn(BinaryReader reader) => new(ReadText(reader, false));

        private static ColumnId ReadOptionalColumn(BinaryReader reader)
        {
            string value = ReadText(reader);
            return value.Length == 0 ? null : new ColumnId(value);
        }

        private static SiteId ReadSite(BinaryReader reader) => new(ReadText(reader, false));

        private static SiteId ReadOptionalSite(BinaryReader reader)
        {
            string value = ReadText(reader);
            return value.Length == 0 ? null : new SiteId(value);
        }

        private static string ReadText(BinaryReader reader, bool allowEmpty = true)
        {
            ushort length = reader.ReadUInt16();
            if (length > 256 || (!allowEmpty && length == 0) || length > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid scene presentation text length.");
            byte[] bytes = reader.ReadBytes(length);
            try
            {
                return MutationValueValidation.StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed scene presentation UTF-8 text.", exception);
            }
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes;
            try
            {
                bytes = MutationValueValidation.StrictUtf8.GetBytes(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed scene presentation UTF-8 text.", exception);
            }

            if (bytes.Length > 256) throw new InvalidDataException("Scene presentation text exceeds its bound.");
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private static void WriteOptionalText(BinaryWriter writer, string value) => WriteText(writer, value ?? string.Empty);
    }
}
