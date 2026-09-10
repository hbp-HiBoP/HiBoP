using System;
using System.IO;
using System.Text;

namespace HBP.Transfer.Anatomy
{
    internal static class AnatomyContactsCodec
    {
        // Decoder resource bounds, never fixture counts or silent truncation.
        internal const int MaximumCount = 100_000;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static int TextBytes(string value) => 4 + Utf8.GetByteCount(value);

        internal static long Length(AnatomyContacts contacts)
        {
            if (contacts.PatientIds.Count > MaximumCount || contacts.Sites.Count > MaximumCount) throw new ArgumentException("Contact count exceeds decoder resource limits.");
            long length = TextBytes(contacts.Implantation) + 9;
            foreach (string id in contacts.PatientIds) length += TextBytes(id);
            foreach (AnatomySite site in contacts.Sites) length += TextBytes(site.Id) + TextBytes(site.Name) + TextBytes(site.Electrode) + 47;
            return length;
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes = Utf8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        internal static void Write(BinaryWriter writer, AnatomyContacts contacts)
        {
            WriteText(writer, contacts.Implantation);
            writer.Write(contacts.RoiActive);
            writer.Write(contacts.PatientIds.Count);
            foreach (string id in contacts.PatientIds) WriteText(writer, id);
            writer.Write(contacts.Sites.Count);
            foreach (AnatomySite site in contacts.Sites)
            {
                WriteText(writer, site.Id);
                WriteText(writer, site.Name);
                WriteText(writer, site.Electrode);
                writer.Write(site.Order);
                writer.Write(site.PatientIndex);
                writer.Write(site.SourceIndex);
                foreach (float value in site.Position.AsReadOnlySpan()) writer.Write(value);
                foreach (float value in site.Color.AsReadOnlySpan()) writer.Write(value);
                writer.Write(site.Diameter);
                writer.Write(site.Visible);
                writer.Write((byte)site.Flags);
                writer.Write(site.EffectiveMasked);
            }
        }

        internal static AnatomyContacts Read(BinaryReader reader, long end)
        {
            string Text()
            {
                int length = reader.ReadInt32();
                if (length < 1 || length > AnatomySnapshotCodec.MaximumTextBytes || length > end - reader.BaseStream.Position) throw new InvalidDataException("Invalid contact text length.");
                string value = Utf8.GetString(reader.ReadBytes(length));
                AnatomySnapshotCodec.ValidateText(value);
                return value;
            }

            bool Boolean()
            {
                byte value = reader.ReadByte();
                if (value > 1) throw new InvalidDataException("Invalid contact boolean.");
                return value == 1;
            }

            int Count(int minimumBytes)
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > MaximumCount || count * (long)minimumBytes > end - reader.BaseStream.Position) throw new InvalidDataException("Invalid contact count or section length.");
                return count;
            }

            string implantation = Text();
            bool roi = Boolean();
            string[] patients = new string[Count(5)];
            for (int i = 0; i < patients.Length; i++) patients[i] = Text();
            AnatomySite[] sites = new AnatomySite[Count(62)];
            for (int i = 0; i < sites.Length; i++)
            {
                string id = Text(), name = Text(), electrode = Text();
                if (end - reader.BaseStream.Position < 47) throw new InvalidDataException("Truncated contact record.");
                int order = reader.ReadInt32(), patient = reader.ReadInt32(), source = reader.ReadInt32();
                float[] position = { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                float[] color = { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                float diameter = reader.ReadSingle();
                bool visible = Boolean();
                var flags = (AnatomySiteFlags)reader.ReadByte();
                sites[i] = new AnatomySite(id, name, electrode, order, patient, source, position, color, diameter, visible, flags, Boolean());
            }

            if (reader.BaseStream.Position != end) throw new InvalidDataException("Contact section length mismatch.");
            return new AnatomyContacts(implantation, roi, patients, sites);
        }
    }
}
