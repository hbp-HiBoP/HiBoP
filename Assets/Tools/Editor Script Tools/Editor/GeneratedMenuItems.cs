using UnityEngine;
using UnityEditor;
using Tools.Unity;

  public static class GeneratedMenuItems {

    [MenuItem("Assets/Create/Custom/C# Class",false,70)]
    private static void Menu0() {
	  ScriptGenerator.CreateFromTemplate("New Class.cs",@"D:\UnityProjects\HBP\Assets\Tools\Editor Script Tools\Templates\Custom\70-C# Class-New Class.cs.txt"); 
    }

    [MenuItem("Assets/Create/Custom/C# Interface",false,71)]
    private static void Menu1() {
	  ScriptGenerator.CreateFromTemplate("New Interface.cs",@"D:\UnityProjects\HBP\Assets\Tools\Editor Script Tools\Templates\Custom\71-C# Interface-New Interface.cs.txt"); 
    }

    [MenuItem("Assets/Create/Custom/C# Struct",false,72)]
    private static void Menu2() {
	  ScriptGenerator.CreateFromTemplate("New Struct.cs",@"D:\UnityProjects\HBP\Assets\Tools\Editor Script Tools\Templates\Custom\72-C# Struct-New Struct.cs.txt"); 
    }

    [MenuItem("Assets/Create/Custom/C# MonoBehaviour",false,73)]
    private static void Menu3() {
	  ScriptGenerator.CreateFromTemplate("New MonoBehaviour.cs",@"D:\UnityProjects\HBP\Assets\Tools\Editor Script Tools\Templates\Custom\73-C# MonoBehaviour-New MonoBehaviour.cs.txt"); 
    }

    [MenuItem("Assets/Create/Custom/C# Scriptable Object",false,74)]
    private static void Menu4() {
	  ScriptGenerator.CreateFromTemplate("New Scriptable Object.cs",@"D:\UnityProjects\HBP\Assets\Tools\Editor Script Tools\Templates\Custom\74-C# Scriptable Object-New Scriptable Object.cs.txt"); 
    }


}
