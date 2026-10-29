using System;
using System.Collections.Generic;
using HBP.Core.Data;
using UnityEngine;

namespace HBP.Quest
{
    public enum QuestSelectionLayout
    {
        Isolated,
        Row,
        Neighbours,
        DepthPair,
        Dense,
        RandomBox
    }

    public static class QuestSelectionLayouts
    {
        public const int RandomSiteCount = 2000;
        public const int RandomSeed = 20261029;
        public static readonly string[] Names = { "Site isolé", "Parcours de 8 sites", "Deux voisins proches", "Deux sites en profondeur", "Groupe dense", "2 000 sites aléatoires" };

        public static readonly string[] Instructions =
        {
            "Atteindre S1 en arrivant devant puis derrière. Observer les corrections de profondeur.",
            "Parcourir R1 à R8 en gardant A appuyé. Privilégier un mouvement fluide.",
            "Alterner N1 / N2. Vérifier que le voisin reste facile à atteindre et à quitter.",
            "Choisir D1 puis D2. Déplacer légèrement la tête pour identifier les deux profondeurs.",
            "Atteindre la cible demandée dans le groupe C. Vérifier l'absence d'accrochage au voisin.",
            "2 000 sites uniformes dans la bbox MNI. Cibles guidées dans la moitié dégagée ; exploration libre du volume complet."
        };

        public static string Prefix(QuestSelectionLayout layout) => new[] { "S", "R", "N", "D", "C", "U" }[(int)layout];

        /// <summary>Convert Unity-space MNI plane geometry into native MNI coordinates, on the removed axial half.</summary>
        public static Patient Patient(Vector3 center, Vector3 normal, Bounds? brainBounds = null)
        {
            Vector3 u = Vector3.Cross(normal, Vector3.up).normalized;
            if (u == Vector3.zero) u = Vector3.right;
            Vector3 v = Vector3.Cross(normal, u).normalized;
            // HBPClipping keeps dot(normal, point - position) >= 0: the positive half is removed.
            Vector3 anchor = center + normal.normalized * 22;
            var sites = new List<HBP.Core.Data.Site>();
            void Add(string name, Vector3 p) => sites.Add(new HBP.Core.Data.Site(name, new[] { new Coordinate("MNI", new Vector3(HBP.Core.DLL.ReferenceSystemConversion.ConvertX(p.x), p.y, p.z), "lab-coordinate-" + name) }, Array.Empty<BaseTagValue>(), "lab-site-" + name));
            Add("S1", anchor);
            for (int i = 0; i < 8; i++) Add("R" + (i + 1), anchor + u * ((i - 3.5f) * 9));
            Add("N1", anchor - u * 2.5f);
            Add("N2", anchor + u * 2.5f);
            Add("D1", anchor - v * 9);
            Add("D2", anchor + v * 9);
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 4; x++)
                Add("C" + (y * 4 + x + 1), anchor + u * ((x - 1.5f) * 6) + v * ((y - 1) * 6));
            if (brainBounds.HasValue)
            {
                // A dedicated generator leaves Unity's global random state untouched.
                // Keep the same cloud across profiles and launches for a fair comparison.
                var random = new System.Random(RandomSeed);
                Vector3 min = brainBounds.Value.min, size = brainBounds.Value.size;
                for (int i = 0; i < RandomSiteCount; i++)
                    Add("U" + (i + 1), min + Vector3.Scale(size, new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())));
            }

            return new Patient { ID = "selection-lab", Name = "Laboratoire", Sites = sites };
        }
    }
}
