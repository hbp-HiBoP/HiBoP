using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    internal static class LegacyPatientElectrodesBridge
    {
        public static IReadOnlyList<LegacyElectrode> LoadPts(string path, string patientName)
        {
            IntPtr list = create_PatientElectrodesList();
            if (list == IntPtr.Zero)
            {
                throw new InvalidOperationException("hbp_export returned a null PatientElectrodesList.");
            }

            try
            {
                int loaded = load_Pts_files_PatientElectrodesList(list, path, string.Empty, patientName, IntPtr.Zero);
                if (loaded != 1)
                {
                    throw new InvalidOperationException("hbp_export failed to load the PTS fixture.");
                }

                int patientCount = patients_nb_PatientElectrodesList(list);
                if (patientCount != 1)
                {
                    throw new InvalidOperationException($"Expected one legacy patient, received {patientCount}.");
                }

                List<LegacyElectrode> electrodes = new();
                int electrodeCount = electrodes_nb_PatientElectrodesList(list, 0);
                for (int electrodeIndex = 0; electrodeIndex < electrodeCount; ++electrodeIndex)
                {
                    List<LegacySite> sites = new();
                    int siteCount = electrode_sites_nb_PatientElectrodesList(list, 0, electrodeIndex);
                    for (int siteIndex = 0; siteIndex < siteCount; ++siteIndex)
                    {
                        float[] position = new float[3];
                        site_pos_PatientElectrodesList(list, 0, electrodeIndex, siteIndex, position);
                        sites.Add(new LegacySite(
                            Marshal.PtrToStringAnsi(site_name_PatientElectrodesList(list, 0, electrodeIndex, siteIndex)) ?? string.Empty,
                            new Vector3(position[0], position[1], position[2])));
                    }

                    electrodes.Add(new LegacyElectrode(
                        Marshal.PtrToStringAnsi(electrode_name_PatientElectrodesList(list, 0, electrodeIndex)) ?? string.Empty,
                        sites));
                }
                return electrodes;
            }
            finally
            {
                delete_PatientElectrodesList(list);
            }
        }

        internal readonly struct LegacyElectrode
        {
            public LegacyElectrode(string name, IReadOnlyList<LegacySite> sites)
            {
                Name = name;
                Sites = sites;
            }

            public string Name { get; }
            public IReadOnlyList<LegacySite> Sites { get; }
        }

        internal readonly struct LegacySite
        {
            public LegacySite(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }
            public Vector3 Position { get; }
        }

        [DllImport("hbp_export", EntryPoint = "create_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create_PatientElectrodesList();

        [DllImport("hbp_export", EntryPoint = "delete_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern void delete_PatientElectrodesList(IntPtr list);

        [DllImport("hbp_export", EntryPoint = "load_Pts_files_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern int load_Pts_files_PatientElectrodesList(IntPtr list, string paths, string marsAtlas, string names, IntPtr marsAtlasIndex);

        [DllImport("hbp_export", EntryPoint = "patients_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern int patients_nb_PatientElectrodesList(IntPtr list);

        [DllImport("hbp_export", EntryPoint = "electrodes_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern int electrodes_nb_PatientElectrodesList(IntPtr list, int patientId);

        [DllImport("hbp_export", EntryPoint = "electrode_sites_nb_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern int electrode_sites_nb_PatientElectrodesList(IntPtr list, int patientId, int electrodeId);

        [DllImport("hbp_export", EntryPoint = "electrode_name_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr electrode_name_PatientElectrodesList(IntPtr list, int patientId, int electrodeId);

        [DllImport("hbp_export", EntryPoint = "site_name_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr site_name_PatientElectrodesList(IntPtr list, int patientId, int electrodeId, int siteId);

        [DllImport("hbp_export", EntryPoint = "site_pos_PatientElectrodesList", CallingConvention = CallingConvention.Cdecl)]
        private static extern void site_pos_PatientElectrodesList(IntPtr list, int patientId, int electrodeId, int siteId, [Out] float[] position);
    }
}
