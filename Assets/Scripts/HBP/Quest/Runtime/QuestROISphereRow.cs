using System.Globalization;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Quest
{
    public sealed class QuestROISphereRow : MonoBehaviour
    {
        [SerializeField] private Text title, definition;
        [SerializeField] private Button remove;
        private Base3DScene scene;
        private ROI roi;
        public Sphere Sphere { get; private set; }

        private void Awake()
        {
            remove.onClick.AddListener(() =>
            {
                if (Sphere != null && QuestROICommands.Contains(scene, roi, Sphere))
                    QuestROICommands.Change(scene, () => roi.RemoveSphere(roi.Spheres.IndexOf(Sphere)));
            });
        }

        public void Bind(Base3DScene source, ROI owner, Sphere sphere)
        {
            scene = source;
            roi = owner;
            Sphere = sphere;
            Refresh();
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            bool valid = Sphere != null && roi != null && roi.Spheres.Contains(Sphere);
            remove.interactable = valid && QuestROICommands.Contains(scene, roi, Sphere);
            if (!valid) return;
            title.text = (roi.Spheres.IndexOf(Sphere) + 1).ToString(CultureInfo.InvariantCulture);
            var position = Sphere.Position;
            definition.text = string.Format(CultureInfo.InvariantCulture, "Position ({0:F1}, {1:F1}, {2:F1}) mm\nRadius {3:F1} mm", position.x, position.y, position.z, Sphere.InfluenceRadius);
        }
    }
}
