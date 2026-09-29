using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HBP.Sync
{
    public enum V2SceneBooleanProperty : byte
    {
        StrongCuts = 1,
        HideBlacklistedSites = 2,
        EdgeMode = 3,
        BrainTransparent = 4,
        DisplayMarsAtlas = 5,
        DisplayJuBrainAtlas = 6,
        AutomaticCutAroundSelectedSite = 7
    }

    public enum V2SceneFloatProperty : byte
    {
        SiteGain = 1,
        BrainAlpha = 2,
        AtlasAlpha = 3
    }

    public enum V2SceneColorProperty : byte
    {
        Brain = 1,
        Cut = 2,
        Colormap = 3
    }

    public enum V2ColumnSpanKind : byte
    {
        Static = 1,
        Dynamic = 2
    }

    public enum V2FunctionalModality : byte
    {
        Fmri = 1,
        Meg = 2
    }

    public sealed class SetSelectedColumn : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public override V2OperationType Type => V2OperationType.SetSelectedColumn;
        public SetSelectedColumn(ColumnId columnId) => ColumnId = columnId;
    }

    public sealed class SetSelectedSite : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public override V2OperationType Type => V2OperationType.SetSelectedSite;

        public SetSelectedSite(ColumnId columnId, SiteId siteId)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId;
        }
    }

    public sealed class SetSceneBoolean : V2Mutation
    {
        public V2SceneBooleanProperty Property { get; }
        public bool Value { get; }
        public override V2OperationType Type => V2OperationType.SetSceneBoolean;

        public SetSceneBoolean(V2SceneBooleanProperty property, bool value)
        {
            if ((byte)property < (byte)V2SceneBooleanProperty.StrongCuts || (byte)property > (byte)V2SceneBooleanProperty.AutomaticCutAroundSelectedSite) throw new ArgumentOutOfRangeException(nameof(property));
            Property = property;
            Value = value;
        }
    }

    public sealed class SetSceneFloat : V2Mutation
    {
        public V2SceneFloatProperty Property { get; }
        public float Value { get; }
        public override V2OperationType Type => V2OperationType.SetSceneFloat;

        public SetSceneFloat(V2SceneFloatProperty property, float value)
        {
            if ((byte)property < (byte)V2SceneFloatProperty.SiteGain || (byte)property > (byte)V2SceneFloatProperty.AtlasAlpha) throw new ArgumentOutOfRangeException(nameof(property));
            V2ValueValidation.ValidateFloat(value, nameof(value));
            if (property != V2SceneFloatProperty.SiteGain && (value < 0f || value > 1f)) throw new ArgumentOutOfRangeException(nameof(value));
            Property = property;
            Value = value;
        }
    }

    public sealed class SetSceneColor : V2Mutation
    {
        public V2SceneColorProperty Property { get; }
        public int Value { get; }
        public override V2OperationType Type => V2OperationType.SetSceneColor;

        public SetSceneColor(V2SceneColorProperty property, int value)
        {
            if ((byte)property < (byte)V2SceneColorProperty.Brain || (byte)property > (byte)V2SceneColorProperty.Colormap) throw new ArgumentOutOfRangeException(nameof(property));
            if (value < 0 || value > 17) throw new ArgumentOutOfRangeException(nameof(value));
            Property = property;
            Value = value;
        }
    }

    public sealed class SetSiteHighlight : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public bool Highlighted { get; }
        public override V2OperationType Type => V2OperationType.SetSiteHighlight;

        public SetSiteHighlight(ColumnId columnId, SiteId siteId, bool highlighted)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            SiteId = siteId ?? throw new ArgumentNullException(nameof(siteId));
            Highlighted = highlighted;
        }
    }

    public sealed class SetSiteLabels : V2Mutation
    {
        private readonly string[] m_Labels;
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public IReadOnlyList<string> Labels => Array.AsReadOnly(m_Labels);
        public override V2OperationType Type => V2OperationType.SetSiteLabels;

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
                m_Labels[i] = V2T09ValueValidation.ValidateRequiredText(m_Labels[i], nameof(labels));
                int length = V2T09ValueValidation.StrictUtf8.GetByteCount(m_Labels[i]);
                bytes = checked(bytes + 2 + length);
            }

            if (bytes > 384) throw new ArgumentOutOfRangeException(nameof(labels), "Ordered labels exceed the bounded mutation payload.");
        }
    }

    public sealed class SetActivityAlpha : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public float Alpha { get; }
        public override V2OperationType Type => V2OperationType.SetActivityAlpha;

        public SetActivityAlpha(ColumnId columnId, float alpha)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            V2T09ValueValidation.ValidateRange(alpha, 0f, 1f, nameof(alpha));
            Alpha = alpha;
        }
    }

    public sealed class SetColumnSpan : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public V2ColumnSpanKind Kind { get; }
        public float Minimum { get; }
        public float Middle { get; }
        public float Maximum { get; }
        public override V2OperationType Type => V2OperationType.SetColumnSpan;

        public SetColumnSpan(ColumnId columnId, V2ColumnSpanKind kind, float minimum, float middle, float maximum)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (kind != V2ColumnSpanKind.Static && kind != V2ColumnSpanKind.Dynamic) throw new ArgumentOutOfRangeException(nameof(kind));
            V2ValueValidation.ValidateFloat(minimum, nameof(minimum));
            V2ValueValidation.ValidateFloat(middle, nameof(middle));
            V2ValueValidation.ValidateFloat(maximum, nameof(maximum));
            if (minimum > middle || middle > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            Kind = kind;
            Minimum = minimum;
            Middle = middle;
            Maximum = maximum;
        }
    }

    public sealed class SetFunctionalDisplay : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public V2FunctionalModality Modality { get; }
        public float NegativeMinimum { get; }
        public float NegativeMaximum { get; }
        public float PositiveMinimum { get; }
        public float PositiveMaximum { get; }
        public bool HideLower { get; }
        public bool HideMiddle { get; }
        public bool HideHigher { get; }
        public override V2OperationType Type => V2OperationType.SetFunctionalDisplay;

        public SetFunctionalDisplay(ColumnId columnId, V2FunctionalModality modality, float negativeMinimum, float negativeMaximum, float positiveMinimum, float positiveMaximum, bool hideLower, bool hideMiddle, bool hideHigher)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (modality != V2FunctionalModality.Fmri && modality != V2FunctionalModality.Meg) throw new ArgumentOutOfRangeException(nameof(modality));
            V2T09ValueValidation.ValidateRange(negativeMinimum, 0f, 1f, nameof(negativeMinimum));
            V2T09ValueValidation.ValidateRange(negativeMaximum, negativeMinimum, 1f, nameof(negativeMaximum));
            V2T09ValueValidation.ValidateRange(positiveMinimum, 0f, 1f, nameof(positiveMinimum));
            V2T09ValueValidation.ValidateRange(positiveMaximum, positiveMinimum, 1f, nameof(positiveMaximum));
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

    public sealed class SetIbcDifumoDisplay : V2Mutation
    {
        public bool IbcEnabled { get; }
        public string IbcContrastReference { get; }
        public bool DifumoEnabled { get; }
        public string DifumoAtlasReference { get; }
        public int DifumoArea { get; }
        public override V2OperationType Type => V2OperationType.SetIbcDifumoDisplay;

        public SetIbcDifumoDisplay(bool ibcEnabled, string ibcContrastReference, bool difumoEnabled, string difumoAtlasReference, int difumoArea)
        {
            IbcContrastReference = V2T09ValueValidation.ValidateOptionalText(ibcContrastReference, nameof(ibcContrastReference));
            DifumoAtlasReference = V2T09ValueValidation.ValidateOptionalText(difumoAtlasReference, nameof(difumoAtlasReference));
            if (difumoArea < 0) throw new ArgumentOutOfRangeException(nameof(difumoArea));
            if (ibcEnabled && IbcContrastReference.Length == 0 || difumoEnabled && DifumoAtlasReference.Length == 0) throw new ArgumentException("An enabled atlas source requires a prepared-resource reference.");
            IbcEnabled = ibcEnabled;
            DifumoEnabled = difumoEnabled;
            DifumoArea = difumoArea;
        }
    }

    public sealed class SetLocalizerDisplay : V2Mutation
    {
        public bool Enabled { get; }
        public string ProtocolReference { get; }
        public string DataReference { get; }
        public string BlocReference { get; }
        public int TimelineIndex { get; }
        public float Minimum { get; }
        public float Middle { get; }
        public float Maximum { get; }
        public override V2OperationType Type => V2OperationType.SetLocalizerDisplay;

        public SetLocalizerDisplay(bool enabled, string protocolReference, string dataReference, string blocReference, int timelineIndex, float minimum, float middle, float maximum)
        {
            ProtocolReference = V2T09ValueValidation.ValidateOptionalText(protocolReference, nameof(protocolReference));
            DataReference = V2T09ValueValidation.ValidateOptionalText(dataReference, nameof(dataReference));
            BlocReference = V2T09ValueValidation.ValidateOptionalText(blocReference, nameof(blocReference));
            if (timelineIndex < 0) throw new ArgumentOutOfRangeException(nameof(timelineIndex));
            V2ValueValidation.ValidateFloat(minimum, nameof(minimum));
            V2ValueValidation.ValidateFloat(middle, nameof(middle));
            V2ValueValidation.ValidateFloat(maximum, nameof(maximum));
            if (minimum > middle || middle > maximum) throw new ArgumentOutOfRangeException(nameof(minimum));
            if (enabled && (ProtocolReference.Length == 0 || DataReference.Length == 0 || BlocReference.Length == 0)) throw new ArgumentException("An enabled localizer requires a prepared protocol, data set and bloc.");
            Enabled = enabled;
            TimelineIndex = timelineIndex;
            Minimum = minimum;
            Middle = middle;
            Maximum = maximum;
        }
    }

    public sealed class SetFmriAtlasCalibration : V2Mutation
    {
        public float Alpha { get; }
        public float NegativeMinimum { get; }
        public float NegativeMaximum { get; }
        public float PositiveMinimum { get; }
        public float PositiveMaximum { get; }
        public override V2OperationType Type => V2OperationType.SetFmriAtlasCalibration;

        public SetFmriAtlasCalibration(float alpha, float negativeMinimum, float negativeMaximum, float positiveMinimum, float positiveMaximum)
        {
            V2T09ValueValidation.ValidateRange(alpha, 0f, 1f, nameof(alpha));
            V2T09ValueValidation.ValidateRange(negativeMinimum, 0f, 1f, nameof(negativeMinimum));
            V2T09ValueValidation.ValidateRange(negativeMaximum, negativeMinimum, 1f, nameof(negativeMaximum));
            V2T09ValueValidation.ValidateRange(positiveMinimum, 0f, 1f, nameof(positiveMinimum));
            V2T09ValueValidation.ValidateRange(positiveMaximum, positiveMinimum, 1f, nameof(positiveMaximum));
            Alpha = alpha;
            NegativeMinimum = negativeMinimum;
            NegativeMaximum = negativeMaximum;
            PositiveMinimum = positiveMinimum;
            PositiveMaximum = positiveMaximum;
        }
    }

    public sealed class SetSelectedRoiSphere : V2Mutation
    {
        public string RoiId { get; }
        public string SphereId { get; }
        public override V2OperationType Type => V2OperationType.SetSelectedRoiSphere;

        public SetSelectedRoiSphere(string roiId, string sphereId)
        {
            RoiId = V2T09ValueValidation.ValidateRequiredText(roiId, nameof(roiId));
            SphereId = V2T09ValueValidation.ValidateOptionalText(sphereId, nameof(sphereId));
        }
    }

    public abstract class V2T09CheckpointRecord
    {
        public abstract V2Mutation Value { get; }
        public ushort RecordType => (ushort)Value.Type;
        public ushort SchemaVersion => 1;
        public byte[] Encode() => V2CheckpointRecordCodec.Encode(RecordType, Value);

        public static V2T09CheckpointRecord FromMutation(V2Mutation mutation) =>
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
                _ => throw new ArgumentException("Mutation is not part of T09.", nameof(mutation))
            };

        public static V2T09CheckpointRecord Decode(byte[] bytes)
        {
            V2Mutation value = V2CheckpointRecordCodec.DecodeAny(bytes);
            return FromMutation(value);
        }
    }

    public abstract class V2T09CheckpointRecord<T> : V2T09CheckpointRecord where T : V2Mutation
    {
        public T TypedValue { get; }
        public sealed override V2Mutation Value => TypedValue;
        protected V2T09CheckpointRecord(T value) => TypedValue = value ?? throw new ArgumentNullException(nameof(value));
    }

    public sealed class SelectedColumnCheckpointRecord : V2T09CheckpointRecord<SetSelectedColumn>
    {
        public SelectedColumnCheckpointRecord(SetSelectedColumn value) : base(value)
        {
        }
    }

    public sealed class SelectedSiteCheckpointRecord : V2T09CheckpointRecord<SetSelectedSite>
    {
        public SelectedSiteCheckpointRecord(SetSelectedSite value) : base(value)
        {
        }
    }

    public sealed class SceneBooleanCheckpointRecord : V2T09CheckpointRecord<SetSceneBoolean>
    {
        public SceneBooleanCheckpointRecord(SetSceneBoolean value) : base(value)
        {
        }
    }

    public sealed class SceneFloatCheckpointRecord : V2T09CheckpointRecord<SetSceneFloat>
    {
        public SceneFloatCheckpointRecord(SetSceneFloat value) : base(value)
        {
        }
    }

    public sealed class SceneColorCheckpointRecord : V2T09CheckpointRecord<SetSceneColor>
    {
        public SceneColorCheckpointRecord(SetSceneColor value) : base(value)
        {
        }
    }

    public sealed class SiteHighlightCheckpointRecord : V2T09CheckpointRecord<SetSiteHighlight>
    {
        public SiteHighlightCheckpointRecord(SetSiteHighlight value) : base(value)
        {
        }
    }

    public sealed class SiteLabelsCheckpointRecord : V2T09CheckpointRecord<SetSiteLabels>
    {
        public SiteLabelsCheckpointRecord(SetSiteLabels value) : base(value)
        {
        }
    }

    public sealed class ActivityAlphaCheckpointRecord : V2T09CheckpointRecord<SetActivityAlpha>
    {
        public ActivityAlphaCheckpointRecord(SetActivityAlpha value) : base(value)
        {
        }
    }

    public sealed class ColumnSpanCheckpointRecord : V2T09CheckpointRecord<SetColumnSpan>
    {
        public ColumnSpanCheckpointRecord(SetColumnSpan value) : base(value)
        {
        }
    }

    public sealed class FunctionalDisplayCheckpointRecord : V2T09CheckpointRecord<SetFunctionalDisplay>
    {
        public FunctionalDisplayCheckpointRecord(SetFunctionalDisplay value) : base(value)
        {
        }
    }

    public sealed class IbcDifumoDisplayCheckpointRecord : V2T09CheckpointRecord<SetIbcDifumoDisplay>
    {
        public IbcDifumoDisplayCheckpointRecord(SetIbcDifumoDisplay value) : base(value)
        {
        }
    }

    public sealed class LocalizerDisplayCheckpointRecord : V2T09CheckpointRecord<SetLocalizerDisplay>
    {
        public LocalizerDisplayCheckpointRecord(SetLocalizerDisplay value) : base(value)
        {
        }
    }

    public sealed class FmriAtlasCalibrationCheckpointRecord : V2T09CheckpointRecord<SetFmriAtlasCalibration>
    {
        public FmriAtlasCalibrationCheckpointRecord(SetFmriAtlasCalibration value) : base(value)
        {
        }
    }

    public sealed class SelectedRoiSphereCheckpointRecord : V2T09CheckpointRecord<SetSelectedRoiSphere>
    {
        public SelectedRoiSphereCheckpointRecord(SetSelectedRoiSphere value) : base(value)
        {
        }
    }

    internal static class V2T09ValueValidation
    {
        internal static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static void ValidateRange(float value, float minimum, float maximum, string name)
        {
            V2ValueValidation.ValidateFloat(value, name);
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

    internal static class V2T09MutationCodec
    {
        internal static V2Mutation ReadBody(BinaryReader reader, V2OperationType type)
        {
            switch (type)
            {
                case V2OperationType.SetSelectedColumn:
                    return new SetSelectedColumn(ReadOptionalColumn(reader));
                case V2OperationType.SetSelectedSite:
                    return new SetSelectedSite(ReadColumn(reader), ReadOptionalSite(reader));
                case V2OperationType.SetSceneBoolean:
                    return new SetSceneBoolean(ReadEnum<V2SceneBooleanProperty>(reader), ReadBoolean(reader));
                case V2OperationType.SetSceneFloat:
                    return new SetSceneFloat(ReadEnum<V2SceneFloatProperty>(reader), reader.ReadSingle());
                case V2OperationType.SetSceneColor:
                    return new SetSceneColor(ReadEnum<V2SceneColorProperty>(reader), reader.ReadInt32());
                case V2OperationType.SetSiteHighlight:
                    return new SetSiteHighlight(ReadColumn(reader), ReadSite(reader), ReadBoolean(reader));
                case V2OperationType.SetSiteLabels:
                    {
                        ColumnId column = ReadColumn(reader);
                        SiteId site = ReadSite(reader);
                        ushort count = reader.ReadUInt16();
                        if (count > 128) throw new InvalidDataException("Too many site labels.");
                        var labels = new string[count];
                        for (int i = 0; i < labels.Length; i++) labels[i] = ReadText(reader, allowEmpty: false);
                        return new SetSiteLabels(column, site, labels);
                    }
                case V2OperationType.SetActivityAlpha:
                    return new SetActivityAlpha(ReadColumn(reader), reader.ReadSingle());
                case V2OperationType.SetColumnSpan:
                    return new SetColumnSpan(ReadColumn(reader), ReadEnum<V2ColumnSpanKind>(reader), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case V2OperationType.SetFunctionalDisplay:
                    {
                        ColumnId column = ReadColumn(reader);
                        V2FunctionalModality modality = ReadEnum<V2FunctionalModality>(reader);
                        float negativeMin = reader.ReadSingle();
                        float negativeMax = reader.ReadSingle();
                        float positiveMin = reader.ReadSingle();
                        float positiveMax = reader.ReadSingle();
                        return new SetFunctionalDisplay(column, modality, negativeMin, negativeMax, positiveMin, positiveMax, ReadBoolean(reader), ReadBoolean(reader), ReadBoolean(reader));
                    }
                case V2OperationType.SetIbcDifumoDisplay:
                    return new SetIbcDifumoDisplay(ReadBoolean(reader), ReadText(reader), ReadBoolean(reader), ReadText(reader), reader.ReadInt32());
                case V2OperationType.SetLocalizerDisplay:
                    return new SetLocalizerDisplay(ReadBoolean(reader), ReadText(reader), ReadText(reader), ReadText(reader), reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case V2OperationType.SetFmriAtlasCalibration:
                    return new SetFmriAtlasCalibration(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case V2OperationType.SetSelectedRoiSphere:
                    return new SetSelectedRoiSphere(ReadText(reader, allowEmpty: false), ReadText(reader));
                default:
                    throw new InvalidDataException("Unsupported T09 mutation type.");
            }
        }

        internal static void WriteBody(BinaryWriter writer, V2Mutation mutation)
        {
            switch (mutation)
            {
                case SetSelectedColumn value: WriteOptionalText(writer, value.ColumnId?.Value); break;
                case SetSelectedSite value:
                    WriteText(writer, value.ColumnId.Value);
                    WriteOptionalText(writer, value.SiteId?.Value);
                    break;
                case SetSceneBoolean value:
                    writer.Write((byte)value.Property);
                    writer.Write((byte)(value.Value ? 1 : 0));
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
                default: throw new ArgumentException("Unsupported T09 mutation type.", nameof(mutation));
            }
        }

        private static T ReadEnum<T>(BinaryReader reader) where T : struct, Enum
        {
            byte value = reader.ReadByte();
            if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException("Invalid T09 enum value.");
            return (T)Enum.ToObject(typeof(T), value);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid T09 boolean value.");
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
            if (length > 256 || (!allowEmpty && length == 0) || length > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid T09 text length.");
            byte[] bytes = reader.ReadBytes(length);
            try
            {
                return V2T09ValueValidation.StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed T09 UTF-8 text.", exception);
            }
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes;
            try
            {
                bytes = V2T09ValueValidation.StrictUtf8.GetBytes(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed T09 UTF-8 text.", exception);
            }

            if (bytes.Length > 256) throw new InvalidDataException("T09 text exceeds its bound.");
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private static void WriteOptionalText(BinaryWriter writer, string value) => WriteText(writer, value ?? string.Empty);
    }
}
