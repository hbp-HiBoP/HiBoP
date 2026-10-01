using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Data.Module3D;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Object3DSite = HBP.Core.Object3D.Site;

namespace HBP.Sync.Scene
{
    public enum V2SiteFilterRequestKind : byte
    {
        Conditions = 1,
        ResetUnmasked = 2,
        ResetAll = 3,
        ChannelSelection = 4
    }

    public sealed class V2SiteFilterChannel
    {
        public string Channel { get; }
        public string PatientId { get; }

        public V2SiteFilterChannel(string channel, string patientId)
        {
            if (string.IsNullOrEmpty(channel)) throw new ArgumentException("A site-filter channel name is required.", nameof(channel));
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("A site-filter patient identity is required.", nameof(patientId));
            Channel = channel;
            PatientId = patientId;
        }
    }

    /// <summary>Immutable snapshot of the user's filtering intent at job acceptance.</summary>
    public sealed class V2SiteFilterRequest
    {
        private static readonly Type[] AllowedConditions =
        {
            typeof(AllFilterCondition), typeof(AnyFilterCondition), typeof(AttributesFilterCondition), typeof(ActivityFilterCondition),
            typeof(MRIMaskFilterCondition), typeof(NameFilterCondition), typeof(PatientNameFilterCondition),
            typeof(RawSitePositionFilterCondition), typeof(SiteTagFilterCondition), typeof(SpecificSiteLocationFilterCondition)
        };

        private readonly BaseFilterCondition[] m_Conditions;
        private readonly V2SiteFilterChannel[] m_Channels;

        public V2SiteFilterRequestKind Kind { get; }
        public IReadOnlyList<BaseFilterCondition> Conditions => Array.AsReadOnly(m_Conditions);
        public IReadOnlyList<V2SiteFilterChannel> Channels => Array.AsReadOnly(m_Channels);

        /// <summary>True when a local UI operation already owns the delayed loading visual.</summary>
        public bool ExternalLoadingIndicator { get; }

        public V2SiteFilterRequest(V2SiteFilterRequestKind kind, IEnumerable<BaseFilterCondition> conditions = null, IEnumerable<V2SiteFilterChannel> channels = null, bool externalLoadingIndicator = false)
        {
            if (kind < V2SiteFilterRequestKind.Conditions || kind > V2SiteFilterRequestKind.ChannelSelection) throw new ArgumentOutOfRangeException(nameof(kind));
            m_Conditions = (conditions ?? Enumerable.Empty<BaseFilterCondition>()).Select(condition => condition == null ? null : (BaseFilterCondition)condition.Clone()).ToArray();
            m_Channels = (channels ?? Enumerable.Empty<V2SiteFilterChannel>()).ToArray();
            if (m_Conditions.Length > 64 || m_Conditions.Any(condition => condition == null)) throw new ArgumentException("A site-filter request can contain at most 64 valid conditions.", nameof(conditions));
            if (m_Channels.Length > SetSiteFilterResult.MaximumSiteCount || m_Channels.Any(channel => channel == null)) throw new ArgumentException("The channel selection exceeds its bound or contains a null value.", nameof(channels));
            if (kind == V2SiteFilterRequestKind.Conditions && m_Conditions.Length == 0) throw new ArgumentException("A condition request requires at least one selected condition.", nameof(conditions));
            if (kind != V2SiteFilterRequestKind.Conditions && m_Conditions.Length != 0) throw new ArgumentException("Only a conditions request may carry filter conditions.", nameof(conditions));
            if (kind == V2SiteFilterRequestKind.Conditions && m_Conditions.Any(condition => !IsAllowedCondition(condition))) throw new ArgumentException("A site-filter request contains a condition that cannot be applied to Site objects.", nameof(conditions));
            if (kind != V2SiteFilterRequestKind.ChannelSelection && m_Channels.Length != 0) throw new ArgumentException("Only a channel selection request may carry channels.", nameof(channels));
            Kind = kind;
            ExternalLoadingIndicator = externalLoadingIndicator;
        }

        public static V2SiteFilterRequest FromConditions(IEnumerable<BaseFilterCondition> conditions, bool externalLoadingIndicator = false) => new(V2SiteFilterRequestKind.Conditions, conditions, externalLoadingIndicator: externalLoadingIndicator);
        public static V2SiteFilterRequest ResetUnmasked(bool externalLoadingIndicator = false) => new(V2SiteFilterRequestKind.ResetUnmasked, externalLoadingIndicator: externalLoadingIndicator);
        public static V2SiteFilterRequest ResetAll(bool externalLoadingIndicator = false) => new(V2SiteFilterRequestKind.ResetAll, externalLoadingIndicator: externalLoadingIndicator);
        public static V2SiteFilterRequest FromChannels(IEnumerable<V2SiteFilterChannel> channels, bool externalLoadingIndicator = false) => new(V2SiteFilterRequestKind.ChannelSelection, channels: channels, externalLoadingIndicator: externalLoadingIndicator);

        internal static bool IsAllowedCondition(BaseFilterCondition condition)
        {
            if (condition == null || !AllowedConditions.Contains(condition.GetType())) return false;
            if (condition is AllFilterCondition all) return all.Conditions != null && all.Conditions.Count >= 2 && all.Conditions.All(IsAllowedCondition);
            if (condition is AnyFilterCondition any) return any.Conditions != null && any.Conditions.Count >= 2 && any.Conditions.All(IsAllowedCondition);
            return true;
        }
    }

    /// <summary>Bounded request codec. The binder only creates known filter-model types.</summary>
    public static class V2SiteFilterRequestCodec
    {
        public const int MaximumEncodedBytes = V2SiteFilterControlCodec.MaximumPayloadBytes - 64;
        private const ushort SchemaVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBFR");

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = FilterConditionBinder.Instance,
            MaxDepth = 24,
            Formatting = Formatting.None
        };

        public static byte[] Encode(V2SiteFilterRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write((byte)request.Kind);
            switch (request.Kind)
            {
                case V2SiteFilterRequestKind.Conditions:
                    string json = JsonConvert.SerializeObject(request.Conditions, JsonSettings);
                    byte[] conditionBytes = Encoding.UTF8.GetBytes(json);
                    if (conditionBytes.Length > MaximumEncodedBytes) throw new InvalidDataException("Serialized site-filter conditions exceed their bound.");
                    writer.Write(checked((uint)conditionBytes.Length));
                    writer.Write(conditionBytes);
                    break;
                case V2SiteFilterRequestKind.ChannelSelection:
                    writer.Write(checked((uint)request.Channels.Count));
                    foreach (V2SiteFilterChannel channel in request.Channels)
                    {
                        WriteText(writer, channel.Channel);
                        WriteText(writer, channel.PatientId);
                    }

                    break;
                case V2SiteFilterRequestKind.ResetUnmasked:
                case V2SiteFilterRequestKind.ResetAll:
                    break;
                default:
                    throw new InvalidDataException("Unsupported site-filter request kind.");
            }

            writer.Flush();
            if (stream.Length > MaximumEncodedBytes) throw new InvalidDataException("Site-filter request exceeds its session-control bound.");
            return stream.ToArray();
        }

        public static V2SiteFilterRequest Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 7 || bytes.Length > MaximumEncodedBytes) throw new InvalidDataException("Invalid site-filter request length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion)
                    throw new InvalidDataException("Unsupported site-filter request signature or schema.");
                byte kindValue = reader.ReadByte();
                if (kindValue < (byte)V2SiteFilterRequestKind.Conditions || kindValue > (byte)V2SiteFilterRequestKind.ChannelSelection)
                    throw new InvalidDataException("Unsupported site-filter request kind.");
                V2SiteFilterRequestKind kind = (V2SiteFilterRequestKind)kindValue;
                V2SiteFilterRequest request;
                switch (kind)
                {
                    case V2SiteFilterRequestKind.Conditions:
                        uint length = reader.ReadUInt32();
                        if (length == 0 || length > MaximumEncodedBytes || length != stream.Length - stream.Position) throw new InvalidDataException("Invalid site-filter condition length.");
                        var conditions = JsonConvert.DeserializeObject<List<BaseFilterCondition>>(Encoding.UTF8.GetString(reader.ReadBytes(checked((int)length))), JsonSettings);
                        if (conditions == null || conditions.Count == 0 || conditions.Count > 64 || conditions.Any(condition => !V2SiteFilterRequest.IsAllowedCondition(condition)))
                            throw new InvalidDataException("Site-filter request contains an unsupported condition.");
                        request = V2SiteFilterRequest.FromConditions(conditions);
                        break;
                    case V2SiteFilterRequestKind.ChannelSelection:
                        uint channelCount = reader.ReadUInt32();
                        if (channelCount > SetSiteFilterResult.MaximumSiteCount) throw new InvalidDataException("Site-filter channel selection exceeds its site-count bound.");
                        var channels = new V2SiteFilterChannel[checked((int)channelCount)];
                        for (int i = 0; i < channels.Length; i++) channels[i] = new V2SiteFilterChannel(ReadText(reader), ReadText(reader));
                        request = V2SiteFilterRequest.FromChannels(channels);
                        break;
                    case V2SiteFilterRequestKind.ResetUnmasked:
                        request = V2SiteFilterRequest.ResetUnmasked();
                        break;
                    case V2SiteFilterRequestKind.ResetAll:
                        request = V2SiteFilterRequest.ResetAll();
                        break;
                    default:
                        throw new InvalidDataException("Unsupported site-filter request kind.");
                }

                if (stream.Position != stream.Length) throw new InvalidDataException("Trailing site-filter request bytes.");
                return request;
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated site-filter request.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid site-filter request value.", exception);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Invalid site-filter condition data.", exception);
            }
        }

        internal static void RebindConditionTags(V2SiteFilterRequest request, Base3DScene scene, bool requireLocalCatalog = false)
        {
            var tags = scene.Columns.SelectMany(column => column.Sites).SelectMany(site => (site.Information.SiteData?.Tags ?? new List<BaseTagValue>()).Select(value => value.Tag).Concat(site.Information.Patient?.Tags?.Select(value => value.Tag) ?? Enumerable.Empty<BaseTag>())).Where(tag => tag != null).GroupBy(tag => tag.ID, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (BaseFilterCondition condition in request.Conditions) RebindConditionTag(condition, tags, requireLocalCatalog);
        }

        private static void RebindConditionTag(BaseFilterCondition condition, IReadOnlyDictionary<string, BaseTag> tags, bool requireLocalCatalog)
        {
            if (condition is AllFilterCondition all)
            {
                foreach (BaseFilterCondition child in all.Conditions) RebindConditionTag(child, tags, requireLocalCatalog);
            }
            else if (condition is AnyFilterCondition any)
            {
                foreach (BaseFilterCondition child in any.Conditions) RebindConditionTag(child, tags, requireLocalCatalog);
            }

            if (condition is SiteTagFilterCondition siteTag)
            {
                siteTag.Tag = ResolveTag(siteTag, tags, requireLocalCatalog);
                RebindEnumValue(siteTag.Value, siteTag.Tag);
            }
        }

        private static BaseTag ResolveTag(object condition, IReadOnlyDictionary<string, BaseTag> tags, bool requireLocalCatalog)
        {
            string id = GetSerializedTagId(condition);
            if (string.IsNullOrEmpty(id)) throw new InvalidDataException("A filter request references an unavailable tag.");
            if (tags.TryGetValue(id, out BaseTag tag)) return tag;
            if (!requireLocalCatalog && condition is SiteTagFilterCondition siteTag && siteTag.Tag != null && StringComparer.Ordinal.Equals(siteTag.Tag.ID, id)) return siteTag.Tag;
            if (PersistentDataManager.IsInitialized && PersistentDataManager.Tags != null && PersistentDataManager.Tags.TryGetTag(id, out tag)) return tag;
            throw new InvalidDataException("A filter request references an unavailable tag.");
        }

        private static string GetSerializedTagId(object condition)
        {
            FieldInfo field = condition.GetType().GetField("m_TagID", BindingFlags.Instance | BindingFlags.NonPublic);
            string id = field?.GetValue(condition) as string;
            if (string.IsNullOrEmpty(id))
            {
                PropertyInfo tagProperty = condition.GetType().GetProperty("Tag", BindingFlags.Instance | BindingFlags.Public);
                if (tagProperty?.GetValue(condition) is BaseTag tag) id = tag.ID;
            }

            return id;
        }

        private static void RebindEnumValue(TagFilterValue value, BaseTag tag)
        {
            if (value is EnumTagFilterValue enumValue)
            {
                if (tag is not EnumTag enumTag || enumValue.Value < 0 || enumValue.Value >= enumTag.Values.Length)
                    throw new InvalidDataException("An enum-tag filter references a value unavailable in this scene.");
                enumValue.SetValue(enumTag, enumValue.Value);
            }
        }

        private static void WriteText(BinaryWriter writer, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (bytes.Length == 0 || bytes.Length > 256) throw new InvalidDataException("Site-filter request text exceeds its bound.");
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        private static string ReadText(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length == 0 || length > 256) throw new InvalidDataException("Invalid site-filter request text length.");
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return new UTF8Encoding(false, true).GetString(bytes);
        }

        private sealed class FilterConditionBinder : ISerializationBinder
        {
            public static readonly FilterConditionBinder Instance = new();

            private static readonly Type[] AllowedTypes =
            {
                typeof(AllFilterCondition), typeof(AnyFilterCondition), typeof(AttributesFilterCondition), typeof(ActivityFilterCondition),
                typeof(MRIMaskFilterCondition), typeof(NameFilterCondition), typeof(PatientNameFilterCondition),
                typeof(RawSitePositionFilterCondition), typeof(SiteTagFilterCondition), typeof(SpecificSiteLocationFilterCondition),
                typeof(BaseFilterCondition), typeof(EmptyTagFilterValue), typeof(BoolTagFilterValue), typeof(StringTagFilterValue),
                typeof(NumberTagFilterValue), typeof(EnumTagFilterValue)
            };

            public Type BindToType(string assemblyName, string typeName)
            {
                Type type = AllowedTypes.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(candidate.FullName, typeName));
                if (type == null || !StringComparer.Ordinal.Equals(type.Assembly.GetName().Name, assemblyName))
                    throw new JsonSerializationException("The site-filter request contains a non-whitelisted type.");
                return type;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (!AllowedTypes.Contains(serializedType)) throw new JsonSerializationException("The site-filter request contains a non-whitelisted type.");
                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
            }
        }
    }

    public static class V2SiteFilterOfflineCapabilityGate
    {
        public static bool CanEvaluate(V2SiteFilterRequest request, Base3DScene scene, V2SceneMutationBoundary boundary, out string explanation)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Kind != V2SiteFilterRequestKind.Conditions)
            {
                explanation = null;
                return true;
            }

            if (!scene)
            {
                explanation = "The prepared local scene is unavailable.";
                return false;
            }

            if (boundary == null)
            {
                explanation = "The prepared local scene boundary is unavailable.";
                return false;
            }

            var sceneSitesByState = scene.Columns.SelectMany(column => column.Sites).GroupBy(site => site.State).ToDictionary(group => group.Key, group => group.First());
            var capturedRoster = boundary.CaptureSiteFilterRoster();
            Object3DSite[] preparedSites = capturedRoster.Targets.Select(target =>
            {
                if (!sceneSitesByState.TryGetValue(target.State, out Object3DSite site))
                    throw new InvalidOperationException("A site in the prepared roster is no longer present in the local scene.");
                return site;
            }).ToArray();
            if (preparedSites.Length == 0)
            {
                explanation = "The prepared local scene has no sites to filter.";
                return false;
            }

            Object3DSite[] evaluationSites = capturedRoster.Targets.Where(target => !target.State.IsMasked).Select(target => sceneSitesByState[target.State]).ToArray();

            foreach (BaseFilterCondition condition in request.Conditions)
            {
                if (!CanEvaluateCondition(condition, evaluationSites, out string reason))
                {
                    explanation = $"The condition '{condition?.GetType().Name ?? "(invalid)"}' cannot be evaluated offline: {reason}";
                    return false;
                }
            }

            try
            {
                V2SiteFilterRequestCodec.RebindConditionTags(request, scene, requireLocalCatalog: true);
            }
            catch (InvalidDataException exception)
            {
                explanation = $"The offline scene cannot resolve a tag used by this filter: {exception.Message}";
                return false;
            }

            explanation = null;
            return true;
        }

        private static bool CanEvaluateCondition(BaseFilterCondition condition, IReadOnlyList<Object3DSite> sites, out string reason)
        {
            if (condition is AllFilterCondition all)
            {
                if (all.Conditions == null || all.Conditions.Count < 2)
                {
                    reason = "an All group must contain at least two conditions.";
                    return false;
                }

                return CanEvaluateChildren(all.Conditions, sites, out reason);
            }

            if (condition is AnyFilterCondition any)
            {
                if (any.Conditions == null || any.Conditions.Count < 2)
                {
                    reason = "an Any group must contain at least two conditions.";
                    return false;
                }

                return CanEvaluateChildren(any.Conditions, sites, out reason);
            }

            if (condition is NameFilterCondition || condition is PatientNameFilterCondition || condition is AttributesFilterCondition || condition is RawSitePositionFilterCondition)
            {
                reason = null;
                return true;
            }

            if (condition is SiteTagFilterCondition siteTag)
            {
                bool tagsAvailable = siteTag.Target switch
                {
                    SiteTagFilterCondition.TargetType.Site => sites.All(site => site.Information?.SiteData?.Tags != null),
                    SiteTagFilterCondition.TargetType.Patient => sites.All(site => site.Information?.Patient?.Tags != null),
                    _ => false
                };
                reason = tagsAvailable ? null : "the selected site or patient tag data is unavailable.";
                return tagsAvailable;
            }

            if (condition is ActivityFilterCondition)
            {
                bool statisticsAvailable = sites.All(site =>
                {
                    var subTrials = site.Statistics?.Trial.ChannelSubTrialBySubBloc;
                    return subTrials != null && subTrials.Values.All(subTrial => subTrial.Values != null);
                });
                reason = statisticsAvailable ? null : "local activity statistics are incomplete.";
                return statisticsAvailable;
            }

            if (condition is SpecificSiteLocationFilterCondition location && location.LocationType == SpecificSiteLocationFilterCondition.SpecificLocationType.RegionOfInterest)
            {
                reason = null;
                return true;
            }

            reason = "it requires an external file, atlas, geometry, data set, or condition type that is not supported by the local evaluator.";
            return false;
        }

        private static bool CanEvaluateChildren(IEnumerable<BaseFilterCondition> children, IReadOnlyList<Object3DSite> sites, out string reason)
        {
            foreach (BaseFilterCondition child in children)
            {
                if (!CanEvaluateCondition(child, sites, out reason)) return false;
            }

            reason = null;
            return true;
        }
    }

    public static class V2SiteFilterEvaluator
    {
        public static async Task<V2SiteFilterEvaluationResult> EvaluateAsync(Base3DScene scene, V2SceneMutationBoundary boundary, V2SiteFilterRequest request, Action<float> reportProgress, CancellationToken stop)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));
            if (request == null) throw new ArgumentNullException(nameof(request));
            V2SiteFilterRequestCodec.RebindConditionTags(request, scene);
            var siteByState = scene.Columns.SelectMany(column => column.Sites).GroupBy(site => site.State).ToDictionary(group => group.Key, group => group.First());
            var capturedRoster = boundary.CaptureSiteFilterRoster();
            SiteTarget[] roster = capturedRoster.Targets.Select(target =>
            {
                if (!siteByState.TryGetValue(target.State, out Object3DSite site))
                    throw new InvalidOperationException("A site in the prepared roster is no longer present in the local scene.");
                return new SiteTarget(target.ColumnId, target.SiteId, site);
            }).ToArray();
            if (roster.Length == 0 || roster.Length > SetSiteFilterResult.MaximumSiteCount) throw new InvalidOperationException("The prepared site roster is empty or exceeds the T12 limit.");
            byte[] rosterHash = capturedRoster.RosterHash;
            var included = roster.Select(target => target.Site.State.IsFiltered).ToArray();
            bool[] wasMasked = roster.Select(target => target.Site.State.IsMasked).ToArray();
            BaseFilterCondition[] conditions = request.Conditions.ToArray();
            BaseFilterCondition[] lifecycleConditions = conditions.SelectMany(EnumerateConditionTree).ToArray();
            int initializedConditionCount = 0;
            try
            {
                if (request.Kind == V2SiteFilterRequestKind.Conditions)
                {
                    foreach (BaseFilterCondition condition in lifecycleConditions)
                    {
                        initializedConditionCount++;
                        condition.BeforeCheck();
                    }
                }

                var selectedChannels = request.Channels.Select(channel => (channel.PatientId, channel.Channel)).ToHashSet();
                for (int i = 0; i < roster.Length; i++)
                {
                    stop.ThrowIfCancellationRequested();
                    Object3DSite site = roster[i].Site;
                    switch (request.Kind)
                    {
                        case V2SiteFilterRequestKind.Conditions:
                            if (!wasMasked[i])
                            {
                                bool matches = true;
                                foreach (BaseFilterCondition condition in conditions) matches &= condition.Check(site);
                                included[i] = matches;
                            }

                            break;
                        case V2SiteFilterRequestKind.ResetUnmasked:
                            if (!wasMasked[i]) included[i] = true;
                            break;
                        case V2SiteFilterRequestKind.ResetAll:
                            included[i] = true;
                            break;
                        case V2SiteFilterRequestKind.ChannelSelection:
                            included[i] = selectedChannels.Contains((site.Information.PatientID, site.name));
                            break;
                    }

                    if ((i & 127) == 127)
                    {
                        reportProgress?.Invoke((float)(i + 1) / roster.Length);
                        await Task.Yield();
                    }
                }

                reportProgress?.Invoke(1f);
                return new V2SiteFilterEvaluationResult(included, rosterHash);
            }
            finally
            {
                for (int i = initializedConditionCount - 1; i >= 0; i--) lifecycleConditions[i].AfterCheck();
            }
        }

        private static IEnumerable<BaseFilterCondition> EnumerateConditionTree(BaseFilterCondition condition)
        {
            yield return condition;
            if (condition is AllFilterCondition all)
            {
                foreach (BaseFilterCondition child in all.Conditions)
                foreach (BaseFilterCondition nested in EnumerateConditionTree(child))
                    yield return nested;
            }
            else if (condition is AnyFilterCondition any)
            {
                foreach (BaseFilterCondition child in any.Conditions)
                foreach (BaseFilterCondition nested in EnumerateConditionTree(child))
                    yield return nested;
            }
        }

        private readonly struct SiteTarget
        {
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }
            public Object3DSite Site { get; }

            public SiteTarget(ColumnId columnId, SiteId siteId, Object3DSite site)
            {
                ColumnId = columnId;
                SiteId = siteId;
                Site = site;
            }
        }
    }

    public sealed class V2SiteFilterEvaluationResult
    {
        private readonly bool[] m_Included;
        private readonly byte[] m_RosterHash;

        public bool[] Included => (bool[])m_Included.Clone();
        public byte[] RosterHash => (byte[])m_RosterHash.Clone();
        public int SiteCount => m_Included.Length;

        public V2SiteFilterEvaluationResult(bool[] included, byte[] rosterHash)
        {
            if (included == null || included.Length == 0 || included.Length > SetSiteFilterResult.MaximumSiteCount)
                throw new ArgumentException("A site-filter evaluation requires a bounded non-empty result mask.", nameof(included));
            if (rosterHash == null || rosterHash.Length != 32)
                throw new ArgumentException("A site-filter evaluation requires a SHA-256 roster identity.", nameof(rosterHash));
            m_Included = (bool[])included.Clone();
            m_RosterHash = (byte[])rosterHash.Clone();
        }
    }

    public static class V2SiteFilterRequestRouter
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<Base3DScene, Registration> Registrations = new();

        public static IDisposable Register(Base3DScene scene, object owner, Func<V2SiteFilterRequest, CancellationToken, Task<bool>> handler)
        {
            if (!scene) throw new ArgumentNullException(nameof(scene));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            lock (Gate)
            {
                if (Registrations.TryGetValue(scene, out Registration existing) && !ReferenceEquals(existing.Owner, owner))
                    throw new InvalidOperationException("A site-filter job handler is already registered for this scene.");
                Registrations[scene] = new Registration(owner, handler);
            }

            return new RegistrationScope(scene, owner);
        }

        public static bool TryGetHandler(Base3DScene scene, out Func<V2SiteFilterRequest, CancellationToken, Task<bool>> handler)
        {
            lock (Gate)
            {
                if (scene && Registrations.TryGetValue(scene, out Registration registration))
                {
                    handler = registration.Handler;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private sealed class Registration
        {
            public object Owner { get; }
            public Func<V2SiteFilterRequest, CancellationToken, Task<bool>> Handler { get; }

            public Registration(object owner, Func<V2SiteFilterRequest, CancellationToken, Task<bool>> handler)
            {
                Owner = owner;
                Handler = handler;
            }
        }

        private sealed class RegistrationScope : IDisposable
        {
            private readonly Base3DScene m_Scene;
            private readonly object m_Owner;

            public RegistrationScope(Base3DScene scene, object owner)
            {
                m_Scene = scene;
                m_Owner = owner;
            }

            public void Dispose()
            {
                lock (Gate)
                    if (!ReferenceEquals(m_Scene, null) && Registrations.TryGetValue(m_Scene, out Registration registration) && ReferenceEquals(registration.Owner, m_Owner))
                        Registrations.Remove(m_Scene);
            }
        }
    }
}
