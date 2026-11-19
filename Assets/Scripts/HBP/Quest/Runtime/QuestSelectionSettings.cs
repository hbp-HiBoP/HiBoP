using System;
using UnityEngine;

namespace HBP.Quest
{
    public enum QuestSelectionMode
    {
        Contact,
        Depth,
        Ray
    }

    public enum QuestCursorStyle
    {
        Sphere,
        Halo,
        Cage,
        Stylus
    }

    public enum QuestCursorColor
    {
        White,
        Amber,
        Cyan
    }

    /// <summary>Quest selection settings. Distances are world metres, independently of anatomy scale.</summary>
    [Serializable]
    public sealed class QuestSelectionSettings
    {
        public QuestSelectionMode Mode;
        public float ContactRadius = .0005f;
        public float MarkerRadius = .0005f;
        public float DepthTolerance = .01f;
        public bool Attraction;
        public float AttractionRadius = .003f;
        public float RayDistance = 1.5f;
        public float RayAngle = 1;
        public bool Stabilize;
        public float SwitchMargin = .15f;
        public bool CandidateCue;
        public bool LinkCue;
        public bool NameCue;
        public float LabelScale = .4f;
        public QuestCursorStyle CursorStyle;
        public QuestCursorColor CursorColor;
        public float CursorGuideRadius = .006f;
        public Vector3 CursorOffset;
        public bool Haptics = true;
        public float HapticAmplitude = .2f;
        public bool Debug;

        public QuestSelectionSettings Copy() => (QuestSelectionSettings)MemberwiseClone();

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(QuestSelectionMode), Mode)) Mode = QuestSelectionMode.Contact;
            ContactRadius = Bound(ContactRadius, .0001f, .02f, .0005f);
            MarkerRadius = Bound(MarkerRadius, .0001f, .02f, .0005f);
            DepthTolerance = Bound(DepthTolerance, 0, .05f, .01f);
            AttractionRadius = Bound(AttractionRadius, 0, .02f, .003f);
            RayDistance = Bound(RayDistance, .05f, 3, 1.5f);
            RayAngle = Bound(RayAngle, 0, 5, 1);
            SwitchMargin = Bound(SwitchMargin, 0, .5f, .15f);
            HapticAmplitude = Bound(HapticAmplitude, 0, 1, .2f);
            // Missing fields in older saved settings can deserialize to zero.
            LabelScale = Bound(LabelScale <= 0 ? .4f : LabelScale, .15f, 1, .4f);
            if (!Enum.IsDefined(typeof(QuestCursorStyle), CursorStyle)) CursorStyle = QuestCursorStyle.Sphere;
            if (!Enum.IsDefined(typeof(QuestCursorColor), CursorColor)) CursorColor = QuestCursorColor.White;
            CursorGuideRadius = Bound(CursorGuideRadius <= 0 ? .006f : CursorGuideRadius, .002f, .02f, .006f);
            CursorOffset = new Vector3(Bound(CursorOffset.x, -.3f, .3f, 0), Bound(CursorOffset.y, -.3f, .3f, 0), Bound(CursorOffset.z, -.3f, .3f, 0));
        }

        public void CopyCursorFrom(QuestSelectionSettings source)
        {
            CursorStyle = source.CursorStyle;
            CursorColor = source.CursorColor;
            CursorGuideRadius = source.CursorGuideRadius;
            CursorOffset = source.CursorOffset;
        }

        private static float Bound(float value, float min, float max, float fallback) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;

        public static readonly string[] ProfileNames = { "Référence", "Visibilité", "Profondeur", "Stabilité", "Rayon", "Préféré (sonde)" };

        public static QuestSelectionSettings Default() => Profile(5);

        public static QuestSelectionSettings Profile(int index)
        {
            var value = new QuestSelectionSettings();
            if (index > 0) value.CandidateCue = value.LinkCue = value.NameCue = true;
            if (index == 2 || index == 3) value.Mode = QuestSelectionMode.Depth;
            if (index == 3) value.Stabilize = true;
            if (index == 4)
            {
                value.Mode = QuestSelectionMode.Ray;
                value.Attraction = true;
            }

            if (index == 5)
            {
                value.Mode = QuestSelectionMode.Depth;
                value.ContactRadius = .003f;
                value.MarkerRadius = .001f;
                value.Attraction = value.Stabilize = true;
                value.CandidateCue = value.LinkCue = false;
                value.CursorStyle = QuestCursorStyle.Halo;
                value.CursorColor = QuestCursorColor.White;
                value.CursorGuideRadius = .004f;
                value.CursorOffset = new Vector3(0, 0, .03f);
            }

            return value;
        }
    }
}
