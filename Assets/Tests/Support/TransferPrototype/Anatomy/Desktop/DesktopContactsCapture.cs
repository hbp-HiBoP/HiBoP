using System;
using System.Linq;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.Transfer.Anatomy.Desktop
{
    internal static class DesktopContactsCapture
    {
        // All reads happen synchronously on the Unity thread with the surface capture.
        internal static AnatomyContacts Capture(Base3DScene scene, Column3D column)
        {
            var implantation = scene.ImplantationManager != null ? scene.ImplantationManager.SelectedImplantation : null;
            int count = column.Sites?.Count ?? 0;
            if (implantation == null)
            {
                if (count != 0) throw new InvalidOperationException("Contacts have no selected implantation.");
                return AnatomyContacts.Empty;
            }

            if (implantation.Name != "MNI" || !implantation.IsLoaded || implantation.SiteInfos.Count != count || (count > 0 && column.RawElectrodes?.NumberOfSites != count))
                throw new InvalidOperationException("Contacts require a complete prepared MNI implantation.");
            bool roi = scene.ROIManager != null && scene.ROIManager.SelectedROI != null;
            var patients = scene.Visualization.Patients;
            var sites = new AnatomySite[count];
            for (int i = 0; i < count; i++)
            {
                var site = column.Sites[i];
                var source = implantation.SiteInfos[i];
                var info = site != null ? site.Information : null;
                if (info == null || site.State == null || info.SiteData == null || info.Index != i || source.PatientIndex < 0 || source.PatientIndex >= patients.Count || patients[source.PatientIndex] != info.Patient || source.Patient != info.Patient || source.SiteData != info.SiteData || source.Index < 0 || source.Index >= info.Patient.Sites.Count || info.Patient.Sites[source.Index] != info.SiteData || source.Name != info.Name || !source.UnityPosition.Equals(info.DefaultPosition))
                    throw new InvalidOperationException("Contact order or implantation association is inconsistent; capture cannot remap native masks silently.");
                Color color;
                float diameter;
                bool visible;
                if (column is Column3DIEEG ieeg)
                {
                    // Scientific inputs, not the last rendered frame or Desktop highlighting.
                    var appearance = ieeg.EvaluateSiteAppearance(i, scene.ShowAllSites, scene.HideBlacklistedSites, scene.IsGeneratorUpToDate);
                    var material = Module3DMain.SharedMaterials.Site.GetSharedMaterial(false, appearance.Type, site.State.Color);
                    if (material == null || !material.HasProperty("_Color"))
                        throw new InvalidOperationException("The scientific site palette requires a material with _Color.");
                    color = material.GetColor("_Color");
                    diameter = 2f * (appearance.Scale * scene.SiteGain);
                    visible = appearance.Visible;
                }
                else
                {
                    var renderer = site.GetComponent<Renderer>();
                    var mesh = site.GetComponent<MeshFilter>()?.sharedMesh;
                    if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_Color") || renderer.HasPropertyBlock() || mesh == null)
                        throw new InvalidOperationException("Contact appearance is not prepared.");
                    Vector3 scale = site.transform.localScale;
                    // SharedMeshes.Site is generated as a radius-one sphere (its coarse bounds are not isotropic).
                    if (scale.x <= 0 || scale.x != scale.y || scale.x != scale.z || mesh != SharedMeshes.Site)
                        throw new InvalidOperationException("Contact capture requires uniformly sized spheres.");
                    color = renderer.sharedMaterial.GetColor("_Color");
                    diameter = 2f * scale.x;
                    visible = site.IsActive && renderer.enabled;
                }

                if (QualitySettings.activeColorSpace == ColorSpace.Linear) color = color.linear;
                AnatomySiteFlags flags = AnatomySiteFlags.None;
                if (site.State.IsMasked) flags |= AnatomySiteFlags.Masked;
                if (site.State.IsBlackListed) flags |= AnatomySiteFlags.Blacklisted;
                if (site.State.IsOutOfROI) flags |= AnatomySiteFlags.OutOfRoi;
                if (site.State.IsFiltered) flags |= AnatomySiteFlags.Filtered;
                Vector3 p = info.DefaultPosition;
                sites[i] = new AnatomySite(info.SiteData.ID, info.Name, source.Electrode, i, source.PatientIndex, source.Index, new[] { p.x, p.y, p.z }, new[] { color.r, color.g, color.b, color.a }, diameter, visible, flags, site.State.IsEffectivelyMasked(roi));
            }

            return new AnatomyContacts(implantation.Name, roi, patients.Select(patient => patient.ID).ToArray(), sites);
        }
    }
}
