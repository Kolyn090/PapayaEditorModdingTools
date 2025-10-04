// File: Editor/CopyEditorTools.cs
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class CopyEditorTools
{
    static CopyEditorTools()
    {
        // Run after domain reload (on project load)
        EditorApplication.delayCall += CopyFiles;
    }

    private static void CopyFiles()
    {
        string sourceFolder = Path.Combine(Application.dataPath, "../Library/PackageCache/com.yourname.editor-tools@1.0.0/Editor");
        string targetFolder = Path.Combine(Application.dataPath, "EditorTools");

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning("Source Editor folder not found!");
            return;
        }

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach (var file in Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = file.Substring(sourceFolder.Length + 1);
            string destPath = Path.Combine(targetFolder, relativePath);

            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(file, destPath, true);
        }

        AssetDatabase.Refresh();
        Debug.Log("Editor scripts copied from package to Assets/EditorTools/");
    }
}
