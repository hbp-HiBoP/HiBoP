using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System.IO;

namespace Tools.Unity
{
    public class MenuGenerator
    {
        /// <summary>
        /// Generates a list of menuitems from an array
        /// </summary>
        [MenuItem("Tools/Templates/Generate Menu Items")]
        static void GenerateMenuItems()
        {
            DirectoryInfo assets = new DirectoryInfo(Application.dataPath);
            DirectoryInfo assetDirectory = assets.GetDirectories("*", SearchOption.AllDirectories).FirstOrDefault((d) => d.Name == "Editor Script Tools");
            string generatedFilePath = assetDirectory.FullName + Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar + "GeneratedMenuItems.cs";

            if (assetDirectory == null || !assetDirectory.Exists) return;

            DirectoryInfo templatesDirectory = new DirectoryInfo(assetDirectory.FullName + Path.DirectorySeparatorChar + "Templates");
            FileInfo[] templates = templatesDirectory.GetFiles("*.txt",SearchOption.AllDirectories);

            // The class string
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using UnityEngine;");
            stringBuilder.AppendLine("using UnityEditor;");
            stringBuilder.AppendLine("using Tools.Unity;");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("  public static class GeneratedMenuItems {");
            stringBuilder.AppendLine("");

            // loops though the templates and generates the menu items
            for (int i = 0; i < templates.Length; i++)
            {
                string template = Path.GetFileNameWithoutExtension(templates[i].Name);
                string[] elements = template.Split('-');
                int order = int.Parse(elements[0]);
                string prefix = templates[i].FullName.Replace(templatesDirectory.FullName, "").Replace(templates[i].Name,"").Substring(1).Replace("\\","/");
                string menuName = elements[1];
                string initialName = elements[2];

                stringBuilder.AppendLine("    [MenuItem(\"Assets/Create/"+ prefix + menuName + "\",false,"+ order +")]");
                stringBuilder.AppendLine("    private static void Menu" + i.ToString() + "() {");
                stringBuilder.AppendLine("	  ScriptGenerator.CreateFromTemplate(\""  +initialName + "\",@\""+ templates[i].FullName+"\"); ");
                stringBuilder.AppendLine("    }");
                stringBuilder.AppendLine("");
            }
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("}");

            // writes the class and imports it so it is visible in the Project window
            File.Delete(generatedFilePath);
            File.WriteAllText(generatedFilePath, stringBuilder.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(generatedFilePath);
            AssetDatabase.Refresh();
        }
    }
}