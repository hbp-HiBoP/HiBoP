#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HBP.Data.Module3D;
using HBP.Quest;
using UnityEngine;

namespace HBP.Tests.Quest
{
    internal static class QuestSceneTestBinding
    {
        public static Base3DScene VisibleScene(GameObject inactiveParent)
        {
            var root = new GameObject("Visible scientific opening");
            root.transform.SetParent(inactiveParent.transform, false);
            var scene = root.AddComponent<Base3DScene>();
            scene.enabled = false;
            typeof(Base3DScene).GetProperty("Visualization").SetValue(scene, new HBP.Core.Data.Visualization());
            return scene;
        }

        public static List<QuestColumnPresentation> Attach(QuestAnatomyView view, Base3DScene scene, QuestColumnDisplayMode mode = QuestColumnDisplayMode.Single)
        {
            var presentation = new QuestAnatomyView.Presentation();
            typeof(QuestAnatomyView.Presentation).GetProperty("DisplayMode").SetValue(presentation, mode);
            typeof(QuestAnatomyView.Presentation).GetProperty("IsPlaced").SetValue(presentation, true);
            var restored = typeof(QuestAnatomyView.Presentation).GetProperty("Restored");
            restored.SetValue(presentation, Activator.CreateInstance(restored.PropertyType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { scene, null, null }, null));
            ((IList)typeof(QuestAnatomyView).GetField("scenes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Add(presentation);
            QuestWindowInteractionTests.Set(view, "current", presentation);
            return (List<QuestColumnPresentation>)typeof(QuestAnatomyView.Presentation).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presentation);
        }

        public static void Column(QuestColumnPresentation presentation, Base3DScene scene, Column3D column)
        {
            QuestWindowInteractionTests.Set(presentation, "scene", scene);
            typeof(QuestColumnPresentation).GetProperty("Column").SetValue(presentation, column);
            presentation.enabled = false;
            if (!scene.Columns.Contains(column)) scene.Columns.Add(column);
        }

        public static void Clear(QuestAnatomyView view)
        {
            ((IList)typeof(QuestAnatomyView).GetField("scenes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view)).Clear();
            QuestWindowInteractionTests.Set(view, "current", null);
        }
    }
}
#endif
