using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tools.Unity.Graph;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class CurveDataCreator
{
#if UNITY_EDITOR
    [MenuItem("Assets/Create/Graph/Data/Curve/Cos")]
#endif
    public static void CreateCos()
    {
        int numberOfPoints = 10000;
        Vector2[] points = new Vector2[numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            float x = 10 * Mathf.PI * ((float) i / (numberOfPoints - 1));
            points[i] = new Vector2(x, Mathf.Cos(x));
        }
        CurveData curve = CurveData.CreateInstance(points, Color.red);

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New cos.asset");
        AssetDatabase.CreateAsset(curve, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = curve;
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Create/Graph/Data/Curve/ComplicatedCurve")]
#endif
    public static void CreateComplicatedCurve()
    {
        int numberOfPoints = 1000000;
        Vector2[] points = new Vector2[numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            float x = 4 * Mathf.PI * ((float)i / (numberOfPoints - 1));
            float y = Mathf.Cos(x) + 0.1f * Mathf.Cos(x * 10) + 0.01f * Mathf.Cos(x * 100) + 0.001f * Mathf.Cos(x * 1000) + 0.0001f * Mathf.Cos(x * 10000) + 0.00005f * Mathf.Cos(x * 100000) + 0.000005f * Mathf.Cos(x * 1000000);
            points[i] = new Vector2(x, y);
        }
        CurveData curve = CurveData.CreateInstance(points, Color.red);

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/ComplicatedCurve.asset");
        AssetDatabase.CreateAsset(curve, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = curve;
    }

#if UNITY_EDITOR
    [MenuItem("Assets/Create/Graph/Data/Curve/ShapedCos")]
#endif
    public static void CreateShapedCos()
    {
        int numberOfPoints = 100;
        Vector2[] points = new Vector2[numberOfPoints];
        float[] shapes = new float[numberOfPoints];
        for (int i = 0; i < numberOfPoints; i++)
        {
            float x = 10 * Mathf.PI * ((float)i / (numberOfPoints - 1));
            points[i] = new Vector2(x, Mathf.Cos(x) + 0.5f * Mathf.Sin(0.3f * x));
            shapes[i] = 0.1f;
        }
        ShapedCurveData curve = ShapedCurveData.CreateInstance(points, shapes, Color.red);

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/ShapedCos.asset");
        AssetDatabase.CreateAsset(curve, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = curve;
    }
}

