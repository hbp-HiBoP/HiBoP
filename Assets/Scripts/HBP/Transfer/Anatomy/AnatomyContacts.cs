using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HBP.Transfer.Anatomy
{
    [Flags]
    public enum AnatomySiteFlags : byte
    {
        None = 0,
        Masked = 1,
        Blacklisted = 2,
        OutOfRoi = 4,
        Filtered = 8
    }

    /// <summary>Immutable prepared contact. Position and diameter use the surface's asset frame/units,
    /// never a Desktop camera or Quest world transform. SourceIndex is the index in the patient's
    /// source site list, not the contact number parsed from Name. Order is the RawSiteList index.</summary>
    public sealed class AnatomySite
    {
        public string Id { get; }
        public string Name { get; }
        public string Electrode { get; }
        public int Order { get; }
        public int PatientIndex { get; }
        public int SourceIndex { get; }
        public AnatomyBuffer<float> Position { get; }
        public AnatomyBuffer<float> Color { get; }
        public float Diameter { get; }
        public bool Visible { get; }
        public AnatomySiteFlags Flags { get; }
        public bool EffectiveMasked { get; }

        public AnatomySite(string id, string name, string electrode, int order, int patientIndex, int sourceIndex, float[] position, float[] color, float diameter, bool visible, AnatomySiteFlags flags, bool effectiveMasked)
        {
            AnatomySnapshotCodec.ValidateText(id);
            AnatomySnapshotCodec.ValidateText(name);
            AnatomySnapshotCodec.ValidateText(electrode);
            if (name.Contains('\0')) throw new ArgumentException("Native site names cannot contain NUL.");
            if (order < 0 || patientIndex < 0 || sourceIndex < 0) throw new ArgumentException("Site indices must be nonnegative.");
            if (position == null || position.Length != 3 || color == null || color.Length != 4) throw new ArgumentException("A site requires XYZ and RGBA.");
            AnatomySnapshot.ValidateFinite(position);
            AnatomySnapshot.ValidateFinite(color);
            if (color.Any(c => c < 0 || c > 1) || float.IsNaN(diameter) || float.IsInfinity(diameter) || diameter <= 0) throw new ArgumentException("Invalid site appearance.");
            if (((byte)flags & ~15) != 0) throw new ArgumentException("Unknown site flags.");
            Id = id;
            Name = name;
            Electrode = electrode;
            Order = order;
            PatientIndex = patientIndex;
            SourceIndex = sourceIndex;
            Position = new AnatomyBuffer<float>((float[])position.Clone());
            Color = new AnatomyBuffer<float>((float[])color.Clone());
            Diameter = diameter;
            Visible = visible;
            Flags = flags;
            EffectiveMasked = effectiveMasked;
        }
    }

    /// <summary>Ordered patient IDs for SetPatients, and ordered sites for AddSite/UpdateMask.
    /// Arrays are copied; immutable records survive producer edits and session disconnection.</summary>
    public sealed class AnatomyContacts
    {
        public const string FrameId = "hibop-mni-unity-mm-v1";
        public static AnatomyContacts Empty { get; } = new AnatomyContacts("MNI", false, Array.Empty<string>(), Array.Empty<AnatomySite>());
        public string Implantation { get; }
        public bool RoiActive { get; }
        public ReadOnlyCollection<string> PatientIds { get; }
        public ReadOnlyCollection<AnatomySite> Sites { get; }

        public AnatomyContacts(string implantation, bool roiActive, string[] patientIds, AnatomySite[] sites)
        {
            AnatomySnapshotCodec.ValidateText(implantation);
            if (patientIds == null || sites == null) throw new ArgumentNullException("Contact arrays cannot be null.");
            var patients = (string[])patientIds.Clone();
            var contacts = (AnatomySite[])sites.Clone();
            var uniquePatients = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in patients)
            {
                AnatomySnapshotCodec.ValidateText(id);
                if (id.Contains('?') || id.Contains('\0') || !uniquePatients.Add(id)) throw new ArgumentException("Patient IDs must be unique and safe for native SetPatients.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var indices = new HashSet<(int, int)>();
            var names = new HashSet<(int, string)>();
            for (int i = 0; i < contacts.Length; i++)
            {
                AnatomySite site = contacts[i];
                if (site == null || site.Order != i || site.PatientIndex >= patients.Length || !ids.Add(site.Id) || !indices.Add((site.PatientIndex, site.SourceIndex)) || !names.Add((site.PatientIndex, site.Name)))
                    throw new ArgumentException("Invalid site order, identity or patient association.");
            }

            Implantation = implantation;
            RoiActive = roiActive;
            PatientIds = Array.AsReadOnly(patients);
            Sites = Array.AsReadOnly(contacts);
        }

        internal void ValidateCoordinates(AnatomyCoordinateSpace coordinates)
        {
            if (Sites.Count == 0) return;
            float[] identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            if (Implantation != "MNI" || coordinates.FrameId != FrameId || coordinates.MappingVersion != 1 || coordinates.Unit != AnatomyLengthUnit.Millimeter || coordinates.Handedness != AnatomyHandedness.Left || !coordinates.AssetToBrain.AsReadOnlySpan().SequenceEqual(identity))
                throw new ArgumentException("Contacts require the shared MNI Unity millimeter anatomical frame with identity mapping; presentation/world coordinates are forbidden.");
        }
    }
}
