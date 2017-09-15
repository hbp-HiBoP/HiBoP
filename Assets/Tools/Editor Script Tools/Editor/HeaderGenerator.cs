using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class HeaderGenerator
{
    #region Properties
	#endregion

	#region Public Methods
    [MenuItem("Assets/Generate Header",false,100)]
    public static void AddHeader()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        AddHeader(assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    [MenuItem("Assets/Generate Header", true, 100)]
    public static bool CanAddHeader()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
        return asset is MonoScript;
    }

    public static void AddHeader(string path)
    {
        StringBuilder stringBuilder;
        using (StreamReader fileStream = new StreamReader(path))
        {
            stringBuilder = new StringBuilder(fileStream.ReadToEnd());
            string header = GenerateHeader();
            stringBuilder.Insert(0, header + System.Environment.NewLine + System.Environment.NewLine);
        }
        using (StreamWriter streamWriter = new StreamWriter(path,false))
        {
            streamWriter.Write(stringBuilder.ToString());
        }
    }
    #endregion

    #region Private Methods
    static string FindHeadersDirectory()
    {
        string[] editorScriptsToolsPaths = Directory.GetDirectories(Application.dataPath, "Editor Script Tools", SearchOption.AllDirectories);
        if (editorScriptsToolsPaths.Length == 0) return string.Empty;
        string[] headersDirectoryPaths = Directory.GetDirectories(editorScriptsToolsPaths[0], "Headers", SearchOption.TopDirectoryOnly);
        return headersDirectoryPaths.Length > 0 ? new DirectoryInfo(headersDirectoryPaths[0]).FullName : string.Empty;
    }
    static string ReadHeader(string directory)
    {
        string result;
        string[] paths = Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
        if(paths.Length > 0)
        {
            string path = paths[0];
            using (StreamReader fileStream = new StreamReader(path))
            {
                result = fileStream.ReadToEnd();
            }
        }
        else
        {
            Debug.LogError("Header not found.");
            result = string.Empty;
        }
        return result;
    }
    static string ReplaceKeyWords(string header)
    {
        StringBuilder stringBuilder = new StringBuilder(header);
        stringBuilder = stringBuilder.Replace("##DATE##", System.DateTime.Now.ToString("yyyy-MM-dd"));
        stringBuilder = stringBuilder.Replace("##UNITYVERSION##", Application.unityVersion);
        stringBuilder = stringBuilder.Replace("##PROJECT##", PlayerSettings.productName);
        string developer = System.Environment.GetEnvironmentVariable("UNITYDEVELOPER");
        if (string.IsNullOrEmpty(developer)) developer = "Unknown";
        stringBuilder = stringBuilder.Replace("##DEVELOPER##", developer);
        return stringBuilder.ToString();
    }
    static string GenerateHeader()
    {
        return ReplaceKeyWords(ReadHeader(FindHeadersDirectory()));
    }
    #endregion
}