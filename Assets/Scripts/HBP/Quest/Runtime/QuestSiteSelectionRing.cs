using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>World-space counterpart of the Desktop selection overlay. The animated image stays local to its billboard.</summary>
    public sealed class QuestSiteSelectionRing : MonoBehaviour
    {
        [SerializeField] private RectTransform billboard;
        [SerializeField] private Image image;
        [SerializeField, Min(0)] private float ratioOffset = .7f;
        [SerializeField, Min(0)] private float minimumAngularDiameterDegrees = .2f;
        private Base3DScene scene;
        private Column3D column;
        private bool hidden;

        public void Bind(Base3DScene source, Column3D owner)
        {
            scene = source;
            column = owner;
            Refresh(Camera.main);
        }

        public void SetHidden(bool value)
        {
            hidden = value;
            Refresh(Camera.main);
        }

        private void LateUpdate() => Refresh(Camera.main);

        public void Refresh(Camera camera)
        {
            if (billboard == null || image == null) return;
            var site = column != null ? column.SelectedSite : null;
            bool visible = !hidden && camera != null && scene != null && !scene.IsClosing && site != null && site.gameObject.activeInHierarchy && site.State != null && SiteAppearance.IsVisible(site.State.IsMasked, site.State.IsOutOfROI, site.State.IsFiltered, site.State.IsBlackListed, scene.ShowAllSites, scene.HideBlacklistedSites);
            billboard.gameObject.SetActive(visible);
            if (!visible) return;

            var sphere = site.GetComponent<SphereCollider>();
            Vector3 center = sphere != null ? site.transform.TransformPoint(sphere.center) : site.transform.position;
            Vector3 scale = site.transform.lossyScale;
            float radius = (sphere != null ? sphere.radius : 1) * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float diameter = 2 * radius * (1 + ratioOffset);
            float minimum = 2 * Vector3.Distance(camera.transform.position, center) * Mathf.Tan(minimumAngularDiameterDegrees * Mathf.Deg2Rad * .5f);
            diameter = Mathf.Max(diameter, minimum);
            billboard.SetPositionAndRotation(center, camera.transform.rotation);
            billboard.GetComponent<Canvas>().worldCamera = camera;
            billboard.sizeDelta = Vector2.one;
            billboard.localScale = Vector3.one;
            float widthScale = billboard.TransformVector(Vector3.right).magnitude;
            float heightScale = billboard.TransformVector(Vector3.up).magnitude;
            billboard.localScale = new Vector3(diameter / Mathf.Max(widthScale, .000001f), diameter / Mathf.Max(heightScale, .000001f), 1);
        }

        private void OnDisable()
        {
            if (billboard != null) billboard.gameObject.SetActive(false);
        }
    }
}
