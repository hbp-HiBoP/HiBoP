using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Tools.Unity
{
	public static class ScriptGenerator 
	{
        #region Properties
        private static Texture2D scriptIcon = (EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D);
        #endregion

        #region Public Methods
        public static void CreateFromTemplate(string initialName, string templatePath)
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateScriptFile>(), initialName, scriptIcon, templatePath);
        }
        #endregion

        #region Private Methods
        private static string ReplaceKeyWords(string file,string className = "")
        {
            StringBuilder stringBuilder = new StringBuilder(file);
            stringBuilder = stringBuilder.Replace("##NAME##", className);
            return stringBuilder.ToString();
        }
        private static string NormalizeClassName(string fileName)
        {
            return fileName.Replace(" ", string.Empty);
        }
        internal static Object CreateScript(string pathName, string templatePath, bool header = true)
        {
            // Assets Info
            string newFilePath = Path.GetFullPath (pathName);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension (pathName);
			string className = NormalizeClassName(fileNameWithoutExtension);

			if (File.Exists(templatePath))
			{
                // Read template.
                string templateText = string.Empty;
                using (StreamReader streamReader = new StreamReader (templatePath))
				{
					templateText = streamReader.ReadToEnd();
				}

                // Work on template.
                templateText = ReplaceKeyWords(templateText, className);
				
                // Write new scripts.
				using (StreamWriter streamWriter = new StreamWriter (newFilePath, false, new UTF8Encoding(true, false)))
				{
					streamWriter.Write (templateText);
				}

                // Add Header.
                if(header) HeaderGenerator.AddHeader(newFilePath);

                // Refresh project tab.
                AssetDatabase.ImportAsset(pathName);
				return AssetDatabase.LoadAssetAtPath (pathName, typeof(Object));
			}
			else
			{
				Debug.LogError(string.Format("The template file was not found: {0}", templatePath));
				return null;
			}
		}
        #endregion

        #region Internal Classes
        private class DoCreateScriptFile : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Object o = CreateScript(pathName, resourceFile);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }
        #endregion
    }
}