using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class Test : EditorWindow
{
	#region Properties
	#endregion

	#region Public Methods
    [MenuItem("Tools/LoadBackground")]
    static void Load()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        byte[] png = sprite.texture.EncodeToPNG();
        File.WriteAllBytes("Assets/mySprite.png", png);
        Debug.Log(sprite);
    }
	#endregion

	#region Private Methods
	void Start ()
	{
	}
	void Update ()
	{
	}
	#endregion
}