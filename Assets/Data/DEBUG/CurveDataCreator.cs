using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tools.Unity.Graph;
using UnityEditor;
using UnityEngine;

public class CurveDataCreator
{
    [MenuItem("Assets/Create/Graph/Data/Curve/Cos")]
    public static void CreateCos()
    {
        Debug.Log("toto");
        int numberOfPoints = 100;
        Vector2[] points = new Vector2[numberOfPoints];
        for (int i = 0; i < points.Length; i++)
        {
            float x = 2 * Mathf.PI * (i / (numberOfPoints - 1));
            points[i] = new Vector2(x, Mathf.Cos(x));
        }
        CurveData curve = new CurveData(points, Color.red);
        curve.name = "titi";

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
        Selection.activeObject = asset;
    }
}

